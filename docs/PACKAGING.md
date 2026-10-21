# Packaging, size and signing / 打包与签名

## Four distinct runtime IDs

- `win-x64`: Windows, self-contained EXE inside a ZIP.
- `linux-x64`: Linux x86-64, self-contained binary inside a tar.gz.
- `osx-arm64`: Mac Apple Silicon, native `.app` inside a compressed DMG.
- `osx-x64`: Mac Intel, native `.app` inside a compressed DMG.

CI runs publish and packaging smoke checks for each RID. The pre-archive publish payloads measured in GitHub Actions are approximately 127 MB on Windows, 45 MB on Linux, and 46–48 MB on macOS. **These are NOT downloadable archive sizes**; check each run's `ls -lh release-assets` line for DMG/ZIP/tar.gz sizes. The final package sizes may change after SDK upgrades.

## Size policy

- `PublishSingleFile=true` and `EnableCompressionInSingleFile=true` compress managed IL and bundled runtime components.
- `IncludeNativeLibrariesForSelfExtract=true` bundles the native graphics libraries with the executable.
- `PublishReadyToRun=false` avoids duplicate native precompiled method bodies.
- `DebugType=None`, `DebugSymbols=false` remove PDB payloads.
- `PublishTrimmed=false`: intentionally conservative. Enable IL trimming **only after GUI smoke tests on all four platforms** and a tested linker configuration. CI build success alone is not proof that trimmed UI resources work.
- No AppImage/Electron/secondary runtime or universal fat macOS app is built: each archive contains **one architecture** only.

## Mac distribution (critical)

The `packaging/macos/package.sh` script constructs `PictureFetcher.app` and verifies its Info.plist and ad-hoc signature before creating a compressed drag-to-Applications DMG.

**Ad-hoc signing does not establish identity.** On macOS downloaded applications may be rejected by Gatekeeper even with an ad-hoc signature; this is not a notarized distribution.

For a seamless end-user install, the repository owner must:

1. Enroll in the Apple Developer Program and obtain a **Developer ID Application** certificate.
2. Sign each embedded native binary and the full `.app` bundle with Hardened Runtime enabled.
3. Submit each DMG to Apple's notary service and staple the notarization ticket.
4. Distribute only the signed and notarized DMG.

The repository **does not contain** private signing identities and should never commit them. The current workflow does not claim notarization or offer a way to bypass Apple's security protections.

## Linux dependencies

A Linux desktop session and system graphics/font libraries are required. Self-contained .NET removes the need for a separate .NET runtime, not platform libraries such as X11, fontconfig, or glibc. The tarball can be installed into a user-writable directory without root privileges.
