dotnet dev-certs https --clean
dotnet dev-certs https -ep certs\aspnetapp.pfx -p mypassword123456789mypassword
dotnet dev-certs https --trust
