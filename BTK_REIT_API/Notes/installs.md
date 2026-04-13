# Project Init Installs:
`dotnet add package Microsoft.EntityFrameworkCore.SqlServer`
`dotnet add package Microsoft.EntityFrameworkCore.Design`

---

# Install the SQL Server provider:
`dotnet add package Microsoft.EntityFrameworkCore.SqlServer`

# Install the code generation tool:
`dotnet add package Microsoft.EntityFrameworkCore.Design`

# Install the global 'dotnet-ef' tool (only if you haven't before):
`dotnet tool install --global dotnet-ef`

# DB Scaffold Command:
`dotnet ef dbcontext scaffold "Data Source=TAZ\SQLEXPRESS;Initial Catalog=REIT;User ID=sa;Password=Pakistan12345;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models`

---

# Trust the local development certificate (not ran):
`dotnet dev-certs https --trust`

---

# This generates the JSON document that describes your API
`dotnet add package Microsoft.AspNetCore.OpenApi`

# This provides the beautiful Scalar interface to view that JSON
`dotnet add package Scalar.AspNetCore`