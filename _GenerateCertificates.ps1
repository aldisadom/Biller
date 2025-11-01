dotnet dev-certs https --clean
dotnet dev-certs https -ep certs\aspnetapp.pfx -p mypassword123456789mypassword
cp certs\aspnetapp.pfx $env:USERPROFILE\.aspnet\https\aspnetapp.pfx
dotnet dev-certs https --trust
