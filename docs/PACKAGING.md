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

The repository **does not contain** private signing identities and should never commit them. To enable signed, notarized GitHub Releases, configure these **GitHub Actions repository secrets**:

| Secret | Description |
| --- | --- |
| `MACOS_CERTIFICATE_BASE64` | Base64-encoded export of your Developer ID Application `.p12` certificate |
| `MACOS_CERTIFICATE_PASSWORD` | Password for the P12 export |
| `MACOS_SIGNING_IDENTITY` | Exact identity name, e.g. `Developer ID Application: Example (TEAMID)` |
| `APPLE_NOTARY_ID` | Apple ID used for notarization |
| `APPLE_NOTARY_PASSWORD` | Apple app-specific password |
| `APPLE_NOTARY_TEAM_ID` | Apple Developer team identifier |

With a certificate, macOS package jobs import the signing identity into a temporary keychain and sign the app with Hardened Runtime and the JIT entitlement. With notarization credentials, the DMG is submitted to Apple's notary service and the accepted ticket is stapled. CI uses ad-hoc signing without secrets, so forks can verify packaging without storing or exposing sensitive credentials.

**Important:** Notarization is required for trusted public distribution and is not guaranteed until those secrets are configured and a tagged release succeeds. Never distribute the signing certificate or password in your repository.

## Linux dependencies

A Linux desktop session and system graphics/font libraries are required. Self-contained .NET removes the need for a separate .NET runtime, not platform libraries such as X11, fontconfig, or glibc. The tarball can be installed into a user-writable directory without root privileges.

## Automated, verifiable release pipeline

- A release branch changes the project's `<Version>` and the single-line `VERSION` file together.
- When `VERSION` changes on `master`, `release.yml` packages all supported RID targets, confirms that all package jobs have passed, creates `vX.Y.Z` on that exact commit, and publishes ZIP/tar.gz and SHA-256 checksums. This avoids a separate manual tag action.
- Existing `vX.Y.Z` tag pushes are still supported. Manually dispatched workflow runs package review artifacts, but do not publish a release.
- macOS builds always produce CI DMGs for validation. A DMG only enters public GitHub Releases if the package script generated a `.dmg.notarized` marker **after** Apple stapler validation. Without Developer ID signing and notarization secrets, public releases intentionally omit both macOS DMGs.
- If either version file and project version disagree, or if an existing release tag points to a different commit, the release job fails rather than silently reusing or moving the tag.
- Only packaged distributables have SHA-256 hashes in `SHA256SUMS.txt`; CI-only macOS builds are excluded from that public list.

### Release recovery

If a release fails *before the version tag is created* (for example, a workflow bug), update the `RELEASE_TRIGGER` file on `master` with a new retry reference. This starts another packaging and release attempt for the same `VERSION` without changing the application version. The release job rejects an existing tag pointing at any other commit rather than rewriting released source history. Before retrying, check that the release tag does not already exist. The official release still requires all platform packaging jobs to pass.
