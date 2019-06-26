# IRC Web Client
A simple IRC client made in C#. The goal of this project was to take an old protocol like IRC and made a more modern client like other chat clients such as Discord. On the networking side the IRC connections are made using asynchronous TCP sockets. The IRC interpreter is a custom one written using C#'s event handling. The UI is rendered to an HTML canvas rather than a text box or a rich text box, giving more control over how the information is displayed.

## Screenshot
![IRC Client](ss1.jpg?raw=true "IRC Client")

![IRC Client](ss2.jpg?raw=true "IRC Client")

## Functionality
* Ability to connect to IRC servers and conversate in various channels
* Channel notify of missed messages
* Saved UI positioning

## Technologies used
* C Sharp
* HTML
* CSS
* JavaScript

## Planned
* Ability to connect to multiple servers at once
* More advanced settings dialog with saved options
* Logging
