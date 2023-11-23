#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install debugging tools
RUN apt-get update \
    && apt-get install -y --no-install-recommends unzip \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["AzDoWorkItemForm.csproj", "."]
RUN dotnet restore "./AzDoWorkItemForm.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "AzDoWorkItemForm.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzDoWorkItemForm.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AzDoWorkItemForm.dll"]