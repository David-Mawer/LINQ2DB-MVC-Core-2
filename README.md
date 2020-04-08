# This is a .Net Core 2.1 Template for Linq2Db

This is the basic MVC template for **.Net Core 2.1** with a few tweaks: -

 - The ORM tool is **Linq2DB**
 - The Identity UI is **fully scaffolded**.
	 - built-in **scaffolding bugs** have been **removed**.
	 - **Identity tables don't have the default names** - these can be changed.
	 - Two-Factor Authentication works (**with the QR Code**).
 - **External Authentication** is easy to setup (documented in Startup.cs). These 3 integrations are coded already: -
	 - Google Authentication
	 - Microsoft Authentication
	 - Facebook Authentication
 
	 Additional External Authentication can be set up through following the comments in Startup.cs
 - Session Tokens are stored in the AspNetUserTokens table - this allows **users to be disconnected remotely** (by deleting their token from the table).
 - SendGrid e-mail logic is in place; you just need to specify your API Key in appsettings.json
 - Linq2DB is fully integrated into the project : -
	 - the default appsettings.json connection string is used to configure Linq2DB
	 - The T4 class generation is installed and configured
	 - Database is SQL Server

# Instructions

 1. Clone or download the project.
 2. Run the "Create Database" script on you local SQL Server instance.
 3. Edit appsettings.json to point to the same server instance where you created the database.
 The project should now run (if the database connections are correctly set-up) .
 
 4. After confirming that the project runs, you can edit the T4 generation file `Data\DBModels.tt` to point to the new database.
 5. Now you can continue with the MVC project developing whatever you had in mind in the first place.
