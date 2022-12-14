# Preamble
This piece of software is a short term solution to the problem that Sonarr currently does not have the `Custom Lists` feature like Radarr does. I already implemented the `Custom Lists` feature into Sonarr with this PR https://github.com/Sonarr/Sonarr/pull/5160 but the feature will only be available in V4 of Sonarr. The answer of when we can expect an approximate release, the answer was: `there is never an ETA for any releases. sometime between tomorrow and the heat death of the universe`

Therefore this project solves the problem of ditching Trakt.TV as a list provider with listrr.pro V2 and not having the `Custom Lists` feature from Radarr to import lists from other sources. You can find the reason for removing Trakt.TV as a list provider here: https://github.com/trakt/api-help/discussions/350

# How does it work?
This container connects to the Sonarr instance defined via environment variables to add all the items from your created listrr.pro V2 lists. You have 2 modes which can be used simultaneously.

### AutoImport
This imports all the lists you have created with your listrr.pro V2 account. You can set the Quality Profile, Root Folder, and Language Profile globally for your account. All the lists in your listrr.pro V2 will be imported with these settings. You just have to set your APIKey that you can find in your user profile by clicking your username.

### Lists
You can add a list by its Id. You can also set the Quality Profile, Root Folder, and Language Profile per each list.


# Examples
Please be sure to only use the `SonarrInstance` settings for the first run. For the first run, you have to get your `LanguageProfiles`, `RootFolders` and `QualityProfiles`. When you set the Url and APIKey of your instance, start the container, and it will show you an output something like this:

```
LOG: Connecting to Sonarr: https://sonarr.mydomain.tld with API Key: 'MY-SONARR-API-KEY'
INFO: Found QualityProfile 1:1080p
INFO: Found RootFolder 5:/mnt/unionfs/Media/TV
INFO: Found LanguageProfile 1:English
```

You can now use this information to set the Ids for your imports.

## docker-compose
``` yaml
version: "3.9"
   
services:
  sonarrbridge:
    image: ghcr.io/theultimatec0der/listrr.pro.sonarr:latest
    environment:
      # This is your Sonarr instance setting
      - SonarrInstance__Url=https://sonarr.mydomain.tld
      - SonarrInstance__ApiKey=YOUR-SONARR-API-KEY
      # This is for automatically importing your own lists from your listrr.pro V2 account
      - listrr__AutoImport__ImportLists=true
      - listrr__AutoImport__ApiKey=YOUR-LISTRR-PRO-V2-API-KEY
      - listrr__AutoImport__QualityProfileId=1
      - listrr__AutoImport__RootFolderId=1
      - listrr__AutoImport__LanguageProfileId=1
      - listrr__AutoImport__Monitored=true
      - listrr__AutoImport__SearchForMissingEpisodes=true
      - listrr__AutoImport__SearchForCutoffUnmetEpisodes=true
      - listrr__AutoImport__SeasonFolder=true
      - listrr__AutoImport__Tags__0=1
      - listrr__AutoImport__Tags__1=5
      # This is for importing listrr.pro V2 lists by their Ids
      - listrr__Lists__0__Id=A-LISTRR-V2-LIST-ID
      - listrr__Lists__0__QualityProfileId=1
      - listrr__Lists__0__RootFolderId=1
      - listrr__Lists__0__LanguageProfileId=1
      - listrr__Lists__0__Monitored=true
      - listrr__Lists__0__SearchForMissingEpisodes=true
      - listrr__Lists__0__SearchForCutoffUnmetEpisodes=true
      - listrr__Lists__0__SeasonFolders=true
      - listrr__Lists__0__Tags__0=1
      - listrr__Lists__0__Tags__1=35
      - listrr__Lists__0__Tags__2=50
      - listrr__Lists__1__Id=A-LISTRR-V2-LIST-ID
      - listrr__Lists__1__QualityProfileId=1
      - listrr__Lists__1__RootFolderId=1
      - listrr__Lists__1__LanguageProfileId=1
      - listrr__Lists__1__Monitored=true
      - listrr__Lists__1__SearchForMissingEpisodes=true
      - listrr__Lists__1__SearchForCutoffUnmetEpisodes=true
      - listrr__Lists__1__SeasonFolders=true
      - listrr__Lists__1__Tags__0=1
      - listrr__Lists__1__Tags__1=3
      - listrr__Lists__1__Tags__2=7
```
