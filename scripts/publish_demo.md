# run Aspire AppHost project
clear
pushd . && cd $(git rev-parse --show-toplevel)

## publish Aspire AppHost project
aspire publish --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj

# Show the generated docker-compose.yaml
bat ./aspire-output/docker-compose.yaml -P

# Show the generated .env file
bat ./aspire-output/.env -P