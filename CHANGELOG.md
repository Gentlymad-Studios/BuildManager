# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2022-11-02
### Added
- Initial release

## [0.1.1] - 2022-12-05
### Added
- added use of a more generic way to create settings files in the user scope instead of the package scope
- removed unneeded .asset

## [0.1.2] - 2022-12-06
### Added
- added default defines for steam and gog
- obsolete targetgroups are now filtered

### Changed
- remove DISABLESTEAMWORKS define from headless default defines
- VersionInfos are now stored in one json instead of multiple txt files

### Fixed
- fixed some UI issues when steam credentials & app config were not provided

## [0.1.3] - 2022-12-07
### Added
- added newtonsoft dependency

### Changed
- switched from JsonUtility to Newtonsoft

## [0.1.4] - 2023-06-14
### Removed
- Removed asset bundle actions and methods from the build process

## [0.1.5] - 2023-06-19
### Added
- added full headless define overwrite support

### Changed
- switched from ScriptableObject Settings to ScriptableSingleton

## [0.1.6] - 2023-08-29
### Fixed
- fix define overwrite

## [0.1.7] - 2023-12-13
### Changed
- move versioninfo to BuildManagerRuntimeSettings

### Added
- add adapter

## [0.1.8] - 2024-04-15
### Added
- add demo option for headless builds

## [0.1.9] - 2024-06-04
### Added
- add before / after build callback for headless builds

## [0.2.0] - 2024-07-19
### Added
- add support to add extra files to the build (after building - before uploading)

## [0.2.1] - 2024-07-19
### Added
- buildID is now extracted in headless builds
