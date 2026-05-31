FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/wwwroot/uploads/avatars

COPY ./publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "OAuth.Server.dll"]