
# Changelog

## [1.2.0] - 2023-07-18

_Do not use version 1.1.1, it has incorrect GUIDs for UdonSharp asset files, breaking references._

### Fixed

- **Breaking:** Restore old, correct GUIDs for asset meta files, fixing references to them ([`555faef`](https://github.com/JanSharp/VRCChalkboard/commit/555faefebccb1d9a835afff04abbd3327b7db6eb))
- Fix exception on build when setting up the ChalkboardManager for the first time ([`b6db98b`](https://github.com/JanSharp/VRCChalkboard/commit/b6db98b1e3db86b125a7aa2e17d529f670d888cf))

## [1.1.1] - 2023-07-17

_First version of this package that is in the VCC listing._

### Changed

- **Breaking:** Separate Chalkboard into its own repo and make it VPM compatible ([`7596ea0`](https://github.com/JanSharp/VRCChalkboard/commit/7596ea0a3e72c13fe93164a74ed75c36b002c1ee), [`7cdb641`](https://github.com/JanSharp/VRCChalkboard/commit/7cdb64100f89b5fe7b11507681b7795d2a4f4f42), [`2ec4771`](https://github.com/JanSharp/VRCChalkboard/commit/2ec4771b6bf31778ca0eaa2d4fd0bfeb5ab488b0), [`eaf8170`](https://github.com/JanSharp/VRCChalkboard/commit/eaf8170420b8f4021aaa257d8282def7662cb2b6))
- **Breaking:** Update OnBuildUtil and other general editor scripting, use SerializedObjects ([`de04745`](https://github.com/JanSharp/VRCChalkboard/commit/de04745880f0ea37345b5fd4e54de94fe7f05368), [`ee4ffb5`](https://github.com/JanSharp/VRCChalkboard/commit/ee4ffb5ffe6218097cd01b94becc93bafb6ad2ca))

### Added

- Add readme with listing link, features and ideas ([`1e9292f`](https://github.com/JanSharp/VRCChalkboard/commit/1e9292f833b9ee7b9bed78670a66415e1d05b895))

## [1.1.0] - 2023-06-09

### Changed

- **Breaking:** Remove and change use of deprecated UdonSharp editor functions ([`e72f369`](https://github.com/JanSharp/VRCChalkboard/commit/e72f369a602ed5b3ab1d218174b674ca8b8eb715))
- **Breaking:** Use refactored OnBuildUtil ([`17dbab8`](https://github.com/JanSharp/VRCChalkboard/commit/17dbab84b8bb6bad192d67607a5f45c8cd000356), [`e72f369`](https://github.com/JanSharp/VRCChalkboard/commit/e72f369a602ed5b3ab1d218174b674ca8b8eb715))
- Migrate to VRChat Creator Companion ([`9ae838c`](https://github.com/JanSharp/VRCChalkboard/commit/9ae838cf1d6280c64c607559fb3ae9967b52bd99), [`78b73b6`](https://github.com/JanSharp/VRCChalkboard/commit/78b73b6816612602b04daafeb4097351f087c01a))

### Fixed

- Fix late joiner syncing with related to VRChat's new server side caching ([`a204797`](https://github.com/JanSharp/VRCChalkboard/commit/a20479706d1fdd3ba683cca5b246c362058840ee), [`6fafaef`](https://github.com/JanSharp/VRCChalkboard/commit/6fafaefd0e7cad7a761252bca79112ef5f465ba5), [`74d6296`](https://github.com/JanSharp/VRCChalkboard/commit/74d62961328f3c3bded29f327360529074952546))

## [1.0.1] - 2022-09-03

### Changed

- Change late joiner sync delay from 10s to 15s after someone joined ([`fb1f91a`](https://github.com/JanSharp/VRCChalkboard/commit/fb1f91abac48beaceda038a6f4a0214c33711e51))
- Put debug log messages behind preprocessor `#if`s to get rid of log message spam in production builds ([`20c8594`](https://github.com/JanSharp/VRCChalkboard/commit/20c85942d332fbd9bca7b42e382d66e8c9de08dd))

### Fixed

- Fix progress bar being full while waiting for late joiner sync to start after joining ([`5d8b2c1`](https://github.com/JanSharp/VRCChalkboard/commit/5d8b2c174753074fba9d79b981bad3bf058beea1))
- Fix potential issues like soft locks with multiple people joining within 10 seconds ([`ea5ad02`](https://github.com/JanSharp/VRCChalkboard/commit/ea5ad02030e4a2ce0ba92135a3025e61223b67f0))

## [1.0.0] - 2022-08-19

### Added

- Add chalks drawing points and lines on chalkboards ([`7d6032c`](https://github.com/JanSharp/VRCChalkboard/commit/7d6032c1c204b1f920e27e4a36392f45b47999e8), [`2e3dc8a`](https://github.com/JanSharp/VRCChalkboard/commit/2e3dc8abd268fc44eabab1595ddc2191321237a3), [`29f465f`](https://github.com/JanSharp/VRCChalkboard/commit/29f465f0aa3e8db45f1e19eb6f80e6a43fc93bd7), [`4fb08c2`](https://github.com/JanSharp/VRCChalkboard/commit/4fb08c2d608a10d63c2b4c6205d20ab9307497b8), [`8d85b85`](https://github.com/JanSharp/VRCChalkboard/commit/8d85b85f9e2de4c885900844a52f1c15dd70f0ed), [`6e93bc3`](https://github.com/JanSharp/VRCChalkboard/commit/6e93bc3e9554c7e897558f6c66d60a9359eb5c2d), [`e019b13`](https://github.com/JanSharp/VRCChalkboard/commit/e019b137ac53d0ee1c20289e21179eef5b492909), [`e89d194`](https://github.com/JanSharp/VRCChalkboard/commit/e89d194a1380107fe9d9ea0de7734eb70c7b8e6d), [`cdfeb18`](https://github.com/JanSharp/VRCChalkboard/commit/cdfeb183da3671848810a69c7da46bb1e7d6ac67), [`5ae8393`](https://github.com/JanSharp/VRCChalkboard/commit/5ae83936e9f3f9264e7cb0ad03c96006719373d9), [`4b38b50`](https://github.com/JanSharp/VRCChalkboard/commit/4b38b504789498cbbb713cfd27a502a65da19872), [`b13f02f`](https://github.com/JanSharp/VRCChalkboard/commit/b13f02fbb2ff6983d30f778f3efb3390b0b05e3a), [`8a81f0c`](https://github.com/JanSharp/VRCChalkboard/commit/8a81f0c1c008d8d29f976781fd4ccbcd0667e12d), [`af9804f`](https://github.com/JanSharp/VRCChalkboard/commit/af9804fb08f2ffa00629c8c1377343df564ce097), [`e89aade`](https://github.com/JanSharp/VRCChalkboard/commit/e89aade39424364048d037e91e44a8c6ac5a3502))
- Implement sponges which are really just chalks with a bigger and square size ([`ea62341`](https://github.com/JanSharp/VRCChalkboard/commit/ea62341b6b352527a0913296a642cb51a9c26ad7))
- Manage texture update rate for drawing, seeing someone draw and being really far away also depending on board texture size ([`835d575`](https://github.com/JanSharp/VRCChalkboard/commit/835d575be329a246e3565a722d18f0d7f1deede7), [`a07272a`](https://github.com/JanSharp/VRCChalkboard/commit/a07272ab30214022364181fc41e2bfa92e59f41e), [`f429e35`](https://github.com/JanSharp/VRCChalkboard/commit/f429e35568178e425f9abe9bc3984e5e4431f9cc))
- Add syncing for both people in the map and late joiners ([`fa73f27`](https://github.com/JanSharp/VRCChalkboard/commit/fa73f27920f6fe82a215b7a0da171fc7c531d6f5), [`81e2cfa`](https://github.com/JanSharp/VRCChalkboard/commit/81e2cfa2c78d70e8103a965c23b3f3efa8b877fd), [`f9cab30`](https://github.com/JanSharp/VRCChalkboard/commit/f9cab30ac756b0e212f7722dd13417e16de026bf), [`51c64ee`](https://github.com/JanSharp/VRCChalkboard/commit/51c64eeff4854adb1ac55545a7ba377047d82052), [`a2b549b`](https://github.com/JanSharp/VRCChalkboard/commit/a2b549ba4fb1a1a7dc33f02ba435713242e56c4d), [`0ff2cd2`](https://github.com/JanSharp/VRCChalkboard/commit/0ff2cd2d7a029842b8c7c1709e4ae4af215729e8), [`6664994`](https://github.com/JanSharp/VRCChalkboard/commit/666499484741cfadc2804446bd9164afbc70b25a), [`a5706c7`](https://github.com/JanSharp/VRCChalkboard/commit/a5706c7dc987571dbf9e5de79c15dae2a48f80e4), [`a396ba9`](https://github.com/JanSharp/VRCChalkboard/commit/a396ba9b3257a34da495877079de62a3a1a48b18), [`17e4cfa`](https://github.com/JanSharp/VRCChalkboard/commit/17e4cfae59820ea8fab2a0dbedceef18f4594028))
- Add optional progress bar for late joiners ([`94efb84`](https://github.com/JanSharp/VRCChalkboard/commit/94efb843b3ac6b072dd7e8fdaccb67e085f5818a))
- Implement board clearing ([`0fbd3b4`](https://github.com/JanSharp/VRCChalkboard/commit/0fbd3b4dfdad6c0847507ecffb07abd7b0652ea3))
- Support using the same chalk on multiple boards ([`a86c5ef`](https://github.com/JanSharp/VRCChalkboard/commit/a86c5ef20a27f29efae2d8cb741a826422e37ae2))
- Support all kinds of rotations of the chalkboard ([`1f46ec9`](https://github.com/JanSharp/VRCChalkboard/commit/1f46ec9d7a4d1677aa073218c418c532e2666a43))
- Make chalk pickup have fixed grip in desktop vs any grip in VR ([`a4def30`](https://github.com/JanSharp/VRCChalkboard/commit/a4def306ba351383f5821b835ab5d9f8b71a59ba))

[1.2.0]: https://github.com/JanSharp/VRCChalkboard/releases/tag/v1.2.0
[1.1.1]: https://github.com/JanSharp/VRCChalkboard/releases/tag/v1.1.1
[1.1.0]: https://github.com/JanSharp/VRCChalkboard/releases/tag/Chalkboard_v1.1.0
[1.0.1]: https://github.com/JanSharp/VRCChalkboard/releases/tag/Chalkboard_v1.0.1
[1.0.0]: https://github.com/JanSharp/VRCChalkboard/releases/tag/Chalkboard_v1.0.0
