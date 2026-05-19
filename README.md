# What is Shoko Server

## shoko-chan fork status

This repository is currently maintained as **shoko-chan**, a working Shoko Server fork focused on scheduler-governed
provider access and provider/resource-safe architecture. The goal is to prove the architecture in a disposable shadow
deployment before considering broader live use or upstream PR work.

The current direction is:

- Route provider-consuming work through the scheduler conveyor belt.
- Govern AniDB HTTP and UDP access through explicit scheduler resource lanes.
- Let manual or user-triggered actions raise scheduler priority without bypassing dispatch/resource governance.
- Keep import, hashing, and provider work observable through queued jobs unless a path is explicitly classified as
  manual, debug, or direct.

## Shadow deployment milestone

The current fork has been validated in a shadow/test deployment on a Windows Docker Desktop host at `10.10.10.32`.

- Server container: `shoko-chan-test`.
- Import source mounted at `/import-anime`.
- Destination/managed folder mounted at `/anime`.
- Files are visible inside the container.
- The scan/import pipeline runs and hashing jobs execute.
- AniDB UDP requests are governed and throttled through the scheduler/resource lanes.
- Queue blocking and draining behaviour has been observed during provider activity.
- The Recently Imported WebUI panel populates after import.

Observed container logs show the behaviour in action: AniDB creator/detail jobs move through the queue while
`UDPRateLimiter` spaces outbound AniDB UDP commands, blocked jobs remain visible, and queued work drains as provider
requests complete.

This is still a shadow deployment milestone, not a production-readiness claim. Do not point this fork at live production
Shoko data until more validation is complete.

## WebUI compatibility

This backend line expects the newer v3 `ManagedFolder` API contract. A stale stable WebUI such as `2.5.4` still calls
the older `ImportFolder` routes and will produce route mismatches during folder actions such as scan.

Use Shoko WebUI `2.6.0-dev.*` for this backend line. The shadow deployment was validated with Shoko WebUI
`2.6.0-dev.26`.

Current caveats:

- AniDB recognition still depends on known file hashes and available release/provider data.
- Shadow deployment uses copied/disposable data only.
- More import, relocation, sync, and long-running queue validation is needed before live migration.

Shoko makes managing your anime collection effortless. It ensures that your collection is well-organized, accessible,
and easy to navigate. Regardless of whether you have a small or large anime collection, Shoko can handle it. Thanks to
its scalability, it can grow alongside your collection with ease.

With Shoko, you'll have access to your entire collection both locally and over the internet, with no additional work
required outside the initial configuration of Shoko Server and one of the supported media player plugins. Say
goodbye to manually inputting information or renaming your files to a specific format just to obtain basic series
data - with Shoko, this is a thing of the past.

Shoko takes the hassle out of managing your anime collection. With its user-friendly interface, you can sit
back and let it do the work for you. No more manual inputting or renaming - just effortless organization and access to
your favorite anime.

[Learn More About Shoko](https://shokoanime.com)
[User Docs](https://docs.shokoanime.com/getting-started/installing-shoko-server)

# Supported Media Players
Shoko currently supports the following media players.

- Plex via **ShokoMetadata** [Download](https://github.com/Cazzar/ShokoMetadata.bundle/releases/) | [Github Repo](https://github.com/Cazzar/ShokoMetadata.bundle)
- Jellyfin via **Shokofin** [Download](https://github.com/ShokoAnime/Shokofin/releases/) | [Github Repo](https://github.com/ShokoAnime/Shokofin)
- Kodi via **Nakamori** [Download](https://shokunin.monogatari.pl/projects/nakamori/nakamori-installation/) | [Github Repo](https://github.com/bigretromike/nakamori/)

Don't see your media player above? If you're a developer who wants to integrate Shoko with a new media player, join our
Discord, and we'll be more than happy to provide guidance and assistance.

**At this time, the Shoko team itself has no plans to integrate Shoko with any other media players.**

# Building Shoko

Install the latest .net sdk

## Windows:
Build TrayService or CLI from VS Code or command line via:

`dotnet build Shoko.TrayService/Shoko.TrayService.csproj`

## Linux:
Install mediainfo and rhash. For apt, that would be:

`sudo apt install mediainfo librhash-dev`


Build from CLI:

`dotnet build -c=Release -r linux-x64 -f net10.0 Shoko.CLI/Shoko.CLI.csproj`

If that doesn't work, this document may be out of date. Check the dockerfile for guaranteedly updated build steps.

# Contributing

We are always accepting help, and there are a million little things that always need done. Hop on our [discord](https://discord.gg/vpeHDsg) and talk to us. Communication is important in any team. No offense, but it's difficult to help anyone that shows up out of nowhere, opens 3 issues, then creates a PR without even talking to us. We have a wealth of experience. Let us help you...preferably before the ADHD takes over, you hyperfixate, and you come up with a fantastic solution to problem that isn't at all what you expected. Support is also best found in the discord, in case you read this far.

![Alt](https://repobeats.axiom.co/api/embed/c233a2de69d1f2f56e4cbe96b4b4cd33dc223d19.svg "Repobeats analytics image")
