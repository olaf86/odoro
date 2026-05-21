# iOS CI/CD plan

This project is a Unity iOS app, so the CI/CD flow has two distinct build products:

1. The Unity project, which should be validated from this repository.
2. The generated iOS Xcode project, which is the input for Apple signing, archive, TestFlight, and App Store delivery.

## Recommended first path

Use GitHub Actions as the source-of-truth CI for Unity work.

- Pull requests to `main` run Unity EditMode tests.
- Manual `workflow_dispatch` builds generate an iOS Xcode project artifact at `Builds/iOS`.
- The generated Xcode project is not committed to `main`.
- App Store signing and TestFlight delivery are added after the Unity iOS artifact builds reliably.

This keeps the generated Xcode project out of normal source control while still making the iOS output reproducible.

## GitHub Actions setup

The initial workflow is in `.github/workflows/unity-ci.yml`.

Required repository secrets:

- `UNITY_LICENSE`: Unity license content used by GameCI.
- `UNITY_EMAIL`: Unity account email, if your license activation path requires it.
- `UNITY_PASSWORD`: Unity account password, if your license activation path requires it.
- `XCODE_CLOUD_PUBLISH_TOKEN`: fine-grained GitHub token with Contents read/write access to the private `olaf86/odoro-ios-xcode` repository.

Current workflow behavior:

- `pull_request` to `main`: run EditMode tests.
- `push` to `main`: run EditMode tests, build the iOS Xcode project, and publish it to the private `olaf86/odoro-ios-xcode` repository.
- Manual run: run EditMode tests, then optionally build the iOS Xcode project artifact.
- Manual run on `main` with `build_ios: true`: publish the generated Xcode project to the private `olaf86/odoro-ios-xcode` repository.

Before publishing can succeed, create the private `olaf86/odoro-ios-xcode` repository once. After that, GitHub Actions owns the repository contents and replaces them with each generated Xcode project.

The publish step retries Git pushes because the generated Xcode project can be large enough for transient GitHub HTTP timeouts. If the first push returns an error after the remote branch was actually updated, the workflow verifies the remote SHA and treats that as success.

Generated iOS projects can include Unity binaries larger than GitHub's normal Git file limit. The publish step automatically tracks files larger than 95 MB with Git LFS in the private Xcode Cloud repository.

Unity can export the shared Xcode scheme with a placeholder buildable name. The publish step normalizes the `Unity-iPhone` scheme to the generated app product name before pushing to the private Xcode Cloud repository.

The publish step also ensures the generated `.xcodeproj` contains the standard `project.xcworkspace/contents.xcworkspacedata` metadata, because Xcode Cloud's workflow setup can fail to discover shared schemes when that project workspace metadata is absent.

The Unity version is pinned to `6000.4.6f1`, matching `ProjectSettings/ProjectVersion.txt`.

## Local batchmode smoke test

On a Mac with the matching Unity editor and iOS build support installed:

```sh
/Applications/Unity/Hub/Editor/6000.4.6f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath "$PWD" \
  -buildTarget iOS \
  -executeMethod Odoro.Editor.CiBuild.BuildIosXcodeProject \
  -ciOutputPath Builds/iOS \
  -ciBuildNumber 1 \
  -logFile Logs/ci-ios-build.log
```

## Xcode Cloud integration options

Xcode Cloud can connect to GitHub, but it wants an Xcode project or workspace to configure and build. For a Unity project, that Xcode project is generated output, not the canonical source.

Recommended options, in order:

1. **GitHub Actions only for iOS delivery**
   Generate the Xcode project, run `xcodebuild archive` on a macOS runner, then upload to App Store Connect. This is the most direct path once signing assets and App Store Connect API credentials are ready.

2. **GitHub Actions plus private Xcode Cloud repository**
   Let GitHub Actions generate the Unity iOS Xcode project and publish it to a dedicated private repository such as `olaf86/odoro-ios-xcode`. Configure Xcode Cloud against that generated Xcode source. This preserves Xcode Cloud's signing/TestFlight workflow without committing generated iOS output to the public/main Unity repository.

   The current workflow publishes to the `main` branch of `olaf86/odoro-ios-xcode`. It is intentionally a separate private repository because it contains generated Xcode project output, not hand-maintained release source. Keep the branch rules light at first, but avoid manual edits; let GitHub Actions own its contents.

3. **Xcode Cloud generates the Unity project**
   Avoid this unless there is a strong reason. Xcode Cloud build machines are optimized for Xcode projects, and installing/running Unity inside Xcode Cloud custom scripts tends to be slower and more fragile.

Apple's GitHub setup requires the person configuring Xcode Cloud to have admin access for a personal repository, or organization owner rights for an organization repository. During setup, install the Xcode Cloud GitHub app only for the repository that needs access.

## Next milestone

After this branch lands:

1. Add Unity license secrets to GitHub.
2. Run the workflow manually and confirm the iOS Xcode artifact is created.
3. Decide whether delivery should be GitHub Actions-only or Xcode Cloud via bridge branch.
4. Add signing and App Store Connect upload once the generated project is stable.
