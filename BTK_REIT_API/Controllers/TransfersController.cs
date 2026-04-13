using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using BTK_REIT_Shared.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/transfers")]
    [Tags("Transfers")]
    public class TransfersController : ControllerBase
    {
        private const int     ReitShareholderId = 1;
        private const decimal ReitMinimumPct    = 10.00m;

        private readonly ReitContext _context;
        private readonly IAuditService _audit;
        private readonly IValidationService _validation;

        public TransfersController(ReitContext context, IAuditService audit, IValidationService validation)
        {
            _context    = context;
            _audit      = audit;
            _validation = validation;
        }

        // ── GET /api/transfers ───────────────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransferDto>>> GetTransfers()
        {
            return await _context.Transfers
                .Include(t => t.FromSh)
                .Include(t => t.ToSh)
                .Include(t => t.Fund)
                .OrderByDescending(t => t.InitiatedDate)
                .ThenByDescending(t => t.TransferId)
                .Select(t => new TransferDto
                {
                    TransferId    = t.TransferId,
                    TransferType  = t.TransferType,
                    ApprovedBy    = t.ApprovedBy,
                    FundId        = t.FundId,
                    FundDtId      = t.FundDtId,
                    FromShId      = t.FromShId,
                    ToShId        = t.ToShId,
                    PctTransfer   = t.PctTransfer,
                    AgreedPrice   = t.AgreedPrice,
                    InitiatedDate = t.InitiatedDate,
                    TransferDate  = t.TransferDate,
                    Status        = t.Status,
                    Notes         = t.Notes,
                    FromShName    = t.FromSh.FullName,
                    ToShName      = t.ToSh.FullName,
                    FundTitle     = t.Fund.FundTitle ?? t.Fund.FundTitle1
                })
                .ToListAsync();
        }

        // ── GET /api/transfers/{id} ──────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TransferDto>> GetTransfer(int id)
        {
            var t = await _context.Transfers
                .Include(t => t.FromSh)
                .Include(t => t.ToSh)
                .Include(t => t.Fund)
                .Where(t => t.TransferId == id)
                .Select(t => new TransferDto
                {
                    TransferId    = t.TransferId,
                    TransferType  = t.TransferType,
                    ApprovedBy    = t.ApprovedBy,
                    FundId        = t.FundId,
                    FundDtId      = t.FundDtId,
                    FromShId      = t.FromShId,
                    ToShId        = t.ToShId,
                    PctTransfer   = t.PctTransfer,
                    AgreedPrice   = t.AgreedPrice,
                    InitiatedDate = t.InitiatedDate,
                    TransferDate  = t.TransferDate,
                    Status        = t.Status,
                    Notes         = t.Notes,
                    FromShName    = t.FromSh.FullName,
                    ToShName      = t.ToSh.FullName,
                    FundTitle     = t.Fund.FundTitle ?? t.Fund.FundTitle1
                })
                .FirstOrDefaultAsync();

            if (t == null)
                return NotFound();

            return t;
        }

        // ── POST /api/transfers/initiate ─────────────────────────────────────
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateTransfer([FromBody] TransferInitiateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.FromShId == dto.ToShId)
                return BadRequest("from_sh_id and to_sh_id must be different shareholders.");

            try
            {
                var (ttValid, ttAllowed) = await _validation.IsValidAsync("transfer_type", dto.TransferType);
                if (!ttValid)
                    return BadRequest(new { error = "Invalid value", field = "transfer_type", allowed = ttAllowed });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }

            if (dto.TransferType.Equals("sale", StringComparison.OrdinalIgnoreCase) && dto.AgreedPrice is null)
                return BadRequest("agreed_price is required when transfer_type is 'sale'.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var sellerStake = await _context.FundDetails
                        .FirstOrDefaultAsync(fd =>
                            fd.FundId  == dto.FundId &&
                            fd.ShId    == dto.FromShId &&
                            fd.EndDate == null);

                    if (sellerStake == null)
                        return NotFound($"No active FundDetails row found for ShareholderID {dto.FromShId} in FundID {dto.FundId}.");

                    if (sellerStake.PctOwned < dto.PctTransfer)
                        return BadRequest(
                            $"Seller (ShareholderID {dto.FromShId}) owns {sellerStake.PctOwned:F2}% " +
                            $"but the transfer requests {dto.PctTransfer:F2}%.");

                    if (dto.FromShId == ReitShareholderId)
                    {
                        decimal reitPostTransfer = sellerStake.PctOwned - dto.PctTransfer;
                        if (reitPostTransfer < ReitMinimumPct)
                            return BadRequest(
                                $"This transfer would leave the REIT with {reitPostTransfer:F2}%, " +
                                $"which is below the required minimum of {ReitMinimumPct:F2}%.");
                    }
                    else
                    {
                        decimal reitCurrentPct = await _context.FundDetails
                            .Where(fd => fd.FundId  == dto.FundId &&
                                         fd.ShId    == ReitShareholderId &&
                                         fd.EndDate == null)
                            .SumAsync(fd => fd.PctOwned);

                        if (reitCurrentPct < ReitMinimumPct)
                            return BadRequest(
                                $"The REIT currently holds only {reitCurrentPct:F2}% in FundID {dto.FundId}, " +
                                $"which already violates the {ReitMinimumPct:F2}% minimum. Resolve before initiating transfers.");
                    }

                    var transfer = new Transfer
                    {
                        FundId        = dto.FundId,
                        FundDtId      = sellerStake.FundDtId,
                        FromShId      = dto.FromShId,
                        ToShId        = dto.ToShId,
                        TransferType  = dto.TransferType.ToLower(),
                        PctTransfer   = dto.PctTransfer,
                        AgreedPrice   = dto.AgreedPrice,
                        ApprovedBy    = dto.UserId,
                        Status        = "pending",
                        InitiatedDate = DateOnly.FromDateTime(DateTime.Today),
                        Notes         = dto.Notes
                    };

                    _context.Transfers.Add(transfer);
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Transfers",
                        recordId: transfer.TransferId,
                        action: $"INSERT: Transfer of {dto.PctTransfer:F2}% in FundID {dto.FundId} " +
                                $"from ShareholderID {dto.FromShId} to ShareholderID {dto.ToShId} initiated. " +
                                $"Type={transfer.TransferType}, AgreedPrice={dto.AgreedPrice?.ToString("F2") ?? "N/A"}.");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        transfer.TransferId,
                        transfer.Status,
                        Message = "Transfer initiated successfully. Call /complete/{id} to execute."
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Transfer initiation failed: {ex.Message}");
                }
            });
        }

        // ── POST /api/transfers/complete/{id} ────────────────────────────────
        [HttpPost("complete/{id:int}")]
        public async Task<IActionResult> CompleteTransfer(
            int id,
            [FromQuery] int userId,
            [FromBody] PaymentCompleteDto? paymentDetails = null)
        {
            if (userId <= 0)
                return BadRequest("userId query parameter is required.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var transfer = await _context.Transfers
                        .FirstOrDefaultAsync(t => t.TransferId == id);

                    if (transfer == null)
                        return NotFound($"Transfer ID {id} not found.");

                    if (!transfer.Status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                        return BadRequest($"Transfer ID {id} has status '{transfer.Status}' and cannot be completed.");

                    var sellerStake = await _context.FundDetails
                        .FirstOrDefaultAsync(fd =>
                            fd.FundDtId == transfer.FundDtId &&
                            fd.EndDate  == null);

                    if (sellerStake == null)
                        return UnprocessableEntity(
                            $"The seller's FundDetails row (FundDtId={transfer.FundDtId}) is no longer active. " +
                            "The transfer cannot be completed.");

                    if (sellerStake.PctOwned < transfer.PctTransfer)
                        return BadRequest(
                            $"Seller now owns {sellerStake.PctOwned:F2}% but the transfer requires {transfer.PctTransfer:F2}%.");

                    DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                    // ── OP 1: Mark transfer as 'completed' ───────────────────
                    string oldTransferStatus = transfer.Status;
                    transfer.Status       = "completed";
                    transfer.TransferDate = today;

                    _audit.LogAction(
                        userId: userId,
                        tableName: "Transfers",
                        recordId: transfer.TransferId,
                        action: $"UPDATE: Transfer ID {transfer.TransferId} status set to 'completed'. " +
                                $"TransferDate={today}.",
                        oldInfo: $"Status={oldTransferStatus}");

                    // ── OP 2: Retire the seller's current FundDetails row ────
                    sellerStake.EndDate = today;

                    _audit.LogAction(
                        userId: userId,
                        tableName: "FundDetails",
                        recordId: sellerStake.FundDtId,
                        action: $"UPDATE: FundDetails row retired for ShareholderID {sellerStake.ShId} " +
                                $"in FundID {sellerStake.FundId}. EndDate set to {today}.",
                        oldInfo: $"PctOwned={sellerStake.PctOwned:F2}, EndDate=null");

                    await _context.SaveChangesAsync();

                    // ── OP 3: New FundDetails row for Seller (reduced stake) ─
                    decimal sellerNewPct = sellerStake.PctOwned - transfer.PctTransfer;

                    FundDetail? sellerNewStake = null;

                    if (sellerNewPct > 0)
                    {
                        decimal sellerNewValue = sellerStake.ShareValue *
                                                 (sellerNewPct / sellerStake.PctOwned);

                        sellerNewStake = new FundDetail
                        {
                            FundId       = transfer.FundId,
                            ShId         = transfer.FromShId,
                            PctOwned     = sellerNewPct,
                            ShareValue   = sellerNewValue,
                            AcquiredDate = today,
                            Notes        = $"Residual stake after TransferID {transfer.TransferId}."
                        };

                        _context.FundDetails.Add(sellerNewStake);
                        await _context.SaveChangesAsync();

                        _audit.LogAction(
                            userId: userId,
                            tableName: "FundDetails",
                            recordId: sellerNewStake.FundDtId,
                            action: $"INSERT: New residual FundDetails for ShareholderID {transfer.FromShId} " +
                                    $"in FundID {transfer.FundId}. " +
                                    $"PctOwned={sellerNewPct:F2}, ShareValue={sellerNewValue:F2}.");
                    }

                    // ── OP 4: New FundDetails row for Buyer ──────────────────
                    decimal transferredValue = sellerStake.ShareValue *
                                               (transfer.PctTransfer / sellerStake.PctOwned);

                    var buyerNewStake = new FundDetail
                    {
                        FundId       = transfer.FundId,
                        ShId         = transfer.ToShId,
                        PctOwned     = transfer.PctTransfer,
                        ShareValue   = transferredValue,
                        AcquiredDate = today,
                        Notes        = $"Acquired via TransferID {transfer.TransferId}."
                    };

                    _context.FundDetails.Add(buyerNewStake);
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: userId,
                        tableName: "FundDetails",
                        recordId: buyerNewStake.FundDtId,
                        action: $"INSERT: New FundDetails for Buyer ShareholderID {transfer.ToShId} " +
                                $"in FundID {transfer.FundId}. " +
                                $"PctOwned={transfer.PctTransfer:F2}, ShareValue={transferredValue:F2}.");

                    // ── FINANCIALS: Payment row for non-gift / non-inheritance ─
                    bool isMonetaryTransfer =
                        !transfer.TransferType.Equals("gift",        StringComparison.OrdinalIgnoreCase) &&
                        !transfer.TransferType.Equals("inheritance", StringComparison.OrdinalIgnoreCase);

                    if (isMonetaryTransfer && transfer.AgreedPrice.HasValue)
                    {
                        if (paymentDetails == null)
                            return BadRequest("paymentDetails body is required for non-gift transfers.");

                        try
                        {
                            var (ptValid, ptAllowed) = await _validation.IsValidAsync("payment_type", paymentDetails.PaymentType);
                            if (!ptValid)
                                return BadRequest(new { error = "Invalid value", field = "payment_type", allowed = ptAllowed });
                        }
                        catch (InvalidOperationException ex)
                        {
                            return StatusCode(500, ex.Message);
                        }

                        decimal grossAmount = transfer.AgreedPrice.Value;
                        decimal tax         = paymentDetails.Tax;
                        decimal additional  = paymentDetails.AdditionalPayments;
                        decimal netDue      = grossAmount + tax + additional;
                        decimal amountPaid  = paymentDetails.AmountPaid;

                        string payStatus = amountPaid >= netDue ? "paid"
                                         : amountPaid > 0      ? "partial"
                                                                : "pending";

                        DateOnly? paymentDate = payStatus == "paid" ? today : null;

                        var payment = new Payment
                        {
                            ShId               = transfer.ToShId,
                            FundId             = transfer.FundId,
                            FundDtId           = buyerNewStake.FundDtId,
                            GrossFundAmount    = grossAmount,
                            Tax                = tax,
                            AdditionalPayments = additional,
                            NetAmountDue       = netDue,
                            AmountPaid         = amountPaid,
                            PaymentDate        = paymentDate,
                            PaymentType        = paymentDetails.PaymentType.ToLower(),
                            Bank               = paymentDetails.Bank,
                            DsNo               = paymentDetails.DsNo,
                            Status             = payStatus,
                            ApprovedBy         = userId,
                            CreationDate       = today,
                            Notes              = paymentDetails.Notes
                                                 ?? $"Payment for TransferID {transfer.TransferId}. " +
                                                    $"AgreedPrice={grossAmount:F2}."
                        };

                        _context.Payments.Add(payment);
                        await _context.SaveChangesAsync();

                        _audit.LogAction(
                            userId: userId,
                            tableName: "Payments",
                            recordId: payment.PaymentId,
                            action: $"INSERT: Created payment record for shareholder {transfer.ToShId} " +
                                    $"— Transfer {transfer.TransferId}. " +
                                    $"Gross={grossAmount:F2}, Tax={tax:F2}, Additional={additional:F2}, " +
                                    $"NetDue={netDue:F2}, AmountPaid={amountPaid:F2}, Status={payStatus}.");
                    }

                    // ── POST-VERIFICATION: ownership must still sum to 100% ───
                    decimal totalPct = await _context.FundDetails
                        .Where(fd => fd.FundId == transfer.FundId && fd.EndDate == null)
                        .SumAsync(fd => fd.PctOwned);

                    if (totalPct != 100.00m)
                        throw new InvalidOperationException(
                            $"Ownership integrity check failed: active stakes in FundID {transfer.FundId} " +
                            $"sum to {totalPct:F2}%, not 100.00%. Transaction rolled back.");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        Message          = "Transfer completed successfully. Ownership integrity verified.",
                        TransferId       = transfer.TransferId,
                        BuyerFundDtId    = buyerNewStake.FundDtId,
                        SellerFundDtId   = sellerNewStake?.FundDtId,
                        VerifiedTotalPct = totalPct
                    });
                }
                catch (InvalidOperationException ioe)
                {
                    await transaction.RollbackAsync();
                    return UnprocessableEntity(ioe.Message);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Transfer completion failed: {ex.Message}");
                }
            });
        }
    }
}
