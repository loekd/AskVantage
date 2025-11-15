#publish

clear
pushd . && cd $(git rev-parse --show-toplevel)

aspire publish -o aspire-output

cat aspire-output/docker-compose.yaml | bat --language yaml -P
cat aspire-output/.env