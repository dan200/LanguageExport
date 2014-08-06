LanguageExport
==============

A command line tool for exporting .lang files used by Minecraft and other games from Google Spreadsheets.

How to use
==========

1. Create a Spreadsheet on Google Drive with Symbol names in the leftmost column and Language codes in the topmost row. See [here](https://docs.google.com/spreadsheets/d/1OHQlhPMk8SwRikQUOI_-TC1NjnfuMb5Y4YTLnId5u98/edit) for a real example from my game [Redirection](http://www.redirectiongame.com).

2. Press the Share button in Google Drive and set the visibility of the document to at least "Anyone with the link can view". Copy the link in the "link to share" box for the next step.

3. From a terminal, run the LanguageExport tool as so:
```
LanguageExport <URL from previous step> <output folder>
```

4. Your output folder should now contain .lang files with all the translations from the Spreadsheet. Re-run this tool whenever you need to update the translations in your game. Grant others edit access to the Spreadsheet to crowdsource your translation!
