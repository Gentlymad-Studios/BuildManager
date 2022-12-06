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