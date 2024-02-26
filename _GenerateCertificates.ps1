dotnet dev-certs https --clean
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\aspnetapp.pfx -p mypassword123456789mypassword
dotnet dev-certs https --trust
