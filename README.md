This is the source code of miscellaneous mods by Destrospean for The Sims 3.

The structure is a bit newish/unorthodox compared to most Sims 3 mod repos, so I'll explain things here:

## Building

This project automatically builds a package that goes in the `dist` folder, but the destination redirects to wherever you put in your Mods folder once you put it there (meaning that you don't have to drag and drop the package :D)

## Folders

Below are folders each respective thing goes in:

### src

This folder contains the actual source code for the project.

### scripts

This folder contains scripts for setting up the package when building the project. None of these should be run directly *except* `PopulateStrings`, as that is used to copy missing strings between English (or whatever the script is set to as the default locale) to other locales.

### strings

This folder is for STBLs in their unprocessed YAML form. It's easier to edit STBLs this way. When the package is built, they get 
"compiled" into STBLs and put into the `resources` folder (more on the below).

As mentioned above, to carry over newly added strings to other locales, run the `PopulateStrings` script in the `scripts` folder.

### resources

This folder contains the resources in their finalized forms (except for the DLLs, which are added a bit differently) to be added to the package. `nameMap.xml` is where you would list these resources. Look at [Arro's repository for ts3buildtool](https://github.com/Paprotk/ts3buildtool) for more info on that.

### tools

This folder contains all the tools for automatically setting up the package.

### libs

This folder contains the unprotected Sims 3 DLLs.
