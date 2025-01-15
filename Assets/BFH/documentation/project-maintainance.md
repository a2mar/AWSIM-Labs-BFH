# Project Maintainance
This project (_AWSIM Labs BFH_) is a fork of _AWSIM Labs_. 

To make the project maintainable with the original project, its `main` branch stay clear of any changes and modifications.

Instead, all Assets, source code, documents, and other resources created for _AWSIM Labs BFH_ are only available to the branch `bfh`(and branches derived of it).

## Developing Guideline
1. **Development branches**<br>
The development of new issues is done in braches with the naming convention `dev/issueName`, which are **directly** branched from the branch `bfh`.
As soon as the work in a development branch is done, and the branch is no longer needed, it should be merged back into `bfh`.
After merging, the branch should be deleted locally and remotely.
2. **Documentation**<br>
When essential changes to the project were applied, the documentation (including the `Edit History` in [README-bfh.md](README-bfh.md)), must be adjusted.
3. **Encapsulation**<br>
To ensure a seamles update process, no Asset, source code, document, or resource from the original project (_AWSIM Labs_) should be changed, if not absolutely necessary.


## Project Updates
To maintain the project and to include future releases of _AWSIM Labs_ in this project, the `main` branched should be updated regularly. After updating the `main` branch, the changes should be made effective in the `bfh` branch.
In order to maintain a linear history, this should be done with a `rebase`.
