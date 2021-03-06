FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /output
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY backend/Whale.MeetingAPI/ Whale.MeetingAPI/
COPY backend/Whale.DAL/ Whale.DAL/
COPY backend/Whale.Shared/ Whale.Shared/
WORKDIR Whale.MeetingAPI
RUN dotnet publish -c Release -o output

FROM base AS final
COPY --from=build /src/Whale.MeetingAPI/output .
ENV ASPNETCORE_URLS=http://+:4202
ENTRYPOINT ["dotnet", "Whale.MeetingAPI.dll"]
