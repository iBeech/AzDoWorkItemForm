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

# Set Azure DevOps environment variables
ENV COMPANY_LOGO=""
ENV AZURE_DEVOPS_PAT=""
ENV AZURE_DEVOPS_ORG_URL=""
ENV PAGE_BACKGROUND_COLOUR=#666
ENV AZURE_DEVOPS_PROJECT=""
ENV WORK_ITEM_TYPE="User Story"
ENV FORM_TITLE="Create Work Item"
ENV FORM_DESCRIPTION=""
ENV DEFAULT_AREA_PATH=""
ENV WORKITEM_SUBMITTED_MESSAGE="Support Request Submitted Successfully!"
ENV TITLE: "System.Title"
ENV DESCRIPTION: "System.Description"
ENV AREA_PATH: "System.AreaPath"
ENV REQUESTER_NAME: ""
ENV DESCRIBE_THE_BUG: ""
ENV STEPS_TO_REPRODUCE: ""
ENV EXPECTED_OUTCOME: ""
ENV ACTUAL_OUTCOME: ""
ENV ADDITIONAL_COMMENTS: ""
ENV SEVERITY: ""
ENV REPRODUCIBILITY: ""
ENV SYSTEM_AREA_OR_SERVICE: ""
ENV AFFECTED_FEATURE_OR_AREA: ""
ENV DEVICE_MODEL_OR_BROWSER: ""
ENV APP_VERSION: ""
ENV OPERATING_SYSTEM: ""
ENV NETWORK_TYPE: ""

ENTRYPOINT ["dotnet", "AzDoWorkItemForm.dll"]