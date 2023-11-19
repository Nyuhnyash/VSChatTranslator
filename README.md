# Chat Translator
Vintage Story mod that allows to translate incoming chat messages into your language 
and reply in chat participants' language.

## Build
- Set `VINTAGE_STORY` environment variable to your game directory.
- Download [dependency](https://github.com/Nyuhnyash/VSChatTranslator/releases/download/1.0.0/GoogleTranslateFreeApi.dll).
```shell
dotnet build
```
- To get ready-to-publish mod as zip archive
```shell
dotnet build -c Release
```

## Commands
| Command                            | Description                                                           |
|------------------------------------|-----------------------------------------------------------------------|
| `.t `(`.translate`) `<message>`    | Translate your text into the target language and send it to the chat. |
| `.ct` (`.chattranslator`)          |                                                                       |
| `.ct toggle`                       | Switch incoming messages translator on and off.                       |
| `.ct lang`                         | List supported languages.                                             |
| `.ct lang <lang type> [lang code]` | View or edit your language preferences.                               |

#### Language types
| Type     | Default     | Description                                                                                                                                                                                           |
|----------|-------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `own`    | game locale | The language in which you prefer to read and write your message if it differs from your game locale.                                                                                                  |
| `source` | `auto`      | The language you expect most of the chat users write. By changing you can improve translation quality bypassing the detection stage, but the translator could fail to translate from other languages. |
| `target` | `en`        | Your messages in `.t` command will be translated to the target language. Most of the time matches with source language, but cannot be `auto`.                                                         |
