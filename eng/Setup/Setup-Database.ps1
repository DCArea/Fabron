
Invoke-Sqlcmd -Query "CREATE DATABASE TGH" -ServerInstance "(localdb)\MSSQLLocalDB"

@("SQLServer-Main.sql", "SQLServer-Persistence.sql", "SQLServer-Reminders.sql") |% {
    Invoke-Sqlcmd -InputFile "eng\Setup\$_" -ServerInstance "(localdb)\MSSQLLocalDB" -Database "TGH"
}
