# Empyrion Chat Auto Translate
## FAQ

Eine übersetzte Version findet ihr im EmpyrionChatAutoTranslate/bin Verzeichnis falls ihr die Mod nicht selber prüfen und compilieren wollt ;-)

Oder hier: https://empyriononline.com/threads/mod-empyrion-chat-auto-translate.45125/

### Wo für ist das?

Chatmeldungen können mit dieser Mod automatisch übersetzt werden. Dazu müssen fremdsprachige Spielern hinterlegt 
in welcher Spache sie kommunizieren z.B. 
	/trans set en
	/trans set de
	/trans set it

Meldungen dieser Spieler werden dann für alle anderen übersetzt und rechts oben in der Infobox angezeigt. Chatmeldungen anderer werden
diesem Spieler in seiner Sprache in der Infobox rechts oben angezeigt.

#### Wie steuert man den MOD?

Die Kommandos funktionieren NUR im Fraktionschat! Die Übersetzung funktioniert sowohl im Globalen- als auch im Fraktionchat.

#### Hilfe

* /trans help : Zeigt die Kommandos der Mod an

#### Übersetzungseinstellungen/Möglichkeiten

* /trans set <language> => Sprache für den Spieler auf 'language' stellen z.B. de, en, it, ...
* /trans help => Liste der Kommandos
* /trans box <text> => Übersetzt den Text in den für den Spieler eingestellte Sprache und zeigt ihn an
* /trans clear => Stellt die Sprache für den Spieler wirder auf die Serversprache zurück
* /trans listall => Listet alle Spracheinstellungen auf (nur ab Moderator erlaubt)

Beispiel (ServerMainLanguage: de -> Serversprache ist deutsch):
- /trans set en

Die Sprache wird gemäss ISO-639-1-Code angegeben https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes

Wenn nun dieser Spieler etwas im Chat schreibt bekommen alle Empänger die eine andere Sprache eingestellt haben oder die (automatisc) 
noch auf Serversprache stehen automatisch einen Hinweis rechts oben mit dem übersetzten Text angezeigt.

Wenn andere Spieler etwas schreiben, bekommt dieser Spieler das ab sofort die Nachichten in der englichen Übersetzung im 
Hinweisfenster angezeigt.

### Konfiguration
Eine Konfiguration kann man in der Datei (wird beim ersten Start automatisch erstellt)

[Empyrion Directory]\Saves\Games\[SaveGameName]\Mods\EmpyrionChatAutoTranslate\ChatAutoTranslatesDB.xml

vornehmen.

* ServerMainLanguage: Allgemeine Zielsprache falls der Spieler keine Sprache eingestellt z.B. de, en, ...
* DefaultSourceLanguage: Standardsprachkennung falls der Spieler keine Sprache eingestellt hat z.B. auto, de, en, ...
* TranslateDisplayTime: Länge in Sekunden die die Übersetzung angezeigt wird
* TranslateDelayTime: Verzögerung zwischen den Übersetzungsanforderungen
* TranslateMinTextLength: Chat muss mindestes auch N Buchstaben bestehen
* TranslateServiceUrl: URL für den Übersetzungsdienst
* TanslateRespose: Übersetzung aus dem Ergebnis ermitteln
* SupressTranslatePrefixes: Wenn die Chatmitteilung mit diesen Zeichen beginnt soll keine Übersetzung gestartet werden

### Was kommt noch?
Zunächst erstmal und damit viel Spaß beim Verstehen wünscht euch

ASTIC/TC

***

English-Version:

---

## FAQ

You can find a translated version in the EmpyrionChatAutoTranslate/bin directory if you do not want to check and compile the mod myself ;-)

Or here: https://empyriononline.com/threads/mod-empyrion-chat-auto-translate.45125/

### What is it for?

Chat messages can be translated automatically with this mod. For this foreign-language players must be deposited
in which language they communicate, e.g.
/trans set
/trans set de
/trans set it

Messages from these players will then be translated for everyone else and displayed in the upper right corner of the info box. Chat messages from others
This player will be displayed in his language in the info box at the top right.

#### How to control the MOD?

The commands work ONLY in the fractional vote! The translation works in both global and faction chat.

#### Help

* /trans help: Displays the commands of the mod

#### Translation Settings / Options

* /trans set <language> => Set language for the player to 'language' e.g. de, en, it, ...
* /trans help => list of commands
* /trans box <text> => Translates the text into the language set for the player and displays it
* /trans clear => Returns the language for the player to the server language
* /trans listall => lists all language settings (only allowed from moderator)

Example (ServerMainLanguage: DE -> server language is German):
- /trans set

The language is specified according to ISO-639-1 code https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes

Now if this player writes something in the chat all recipients who have set a different language or (automatic)
still on server language are automatically a note top right with the translated text displayed.

If other players write something, this player gets the now in the English translation
Note window is displayed.

### configuration
A configuration can be found in the file (automatically created on first startup)

[Empyrion Directory]\Saves\Games\[SaveGameName]\Mods\EmpyrionChatAutoTranslate\ChatAutoTranslatesDB.xml

make.

* ServerMainLanguage: General target language if the player does not set a language e.g. de, en, ...
* DefaultSourceLanguage: default language identifier if the player has not set a language e.g. car, de, en, ...
* TranslateDisplayTime: Length in seconds that the translation is displayed
* TranslateDelayTime: Delay each translation request
* TranslateMinTextLength: Chat must have N letter characters
* TranslateServiceUrl: URL for the translation service
* TanslateRespose: Find translation from the result
* SupressTranslatePrefixes: If the chat message begins with these characters no translation should be started

### What else is coming?
First of all, and have fun with understanding then wish you

ASTIC / TC