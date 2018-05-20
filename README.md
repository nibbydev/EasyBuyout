# Path of Exile item pricer

![Item pricer v0.9](screenshots/action.gif)

## What does it do
When the user right-clicks an item in a premium tab (or any tab), this handy program instantly outputs a buyout note containing the average price for that type of item.

## Who is it for
Players who are tired of induvidually pricing hundreds of items in their stash tabs. 
Be you a labrunner or an Uber Atziri farmer, who has dozens upon dozens of gems or maps that need prices or maybe just your average player who wants to actually enjoy the game instead of playing Shoppe Keep all day long. Well, then this program is perfect for you.

## How to get it running (as of v1.0.14)
1. Compile it yourself or download a compiled file from the [releases](https://github.com/siegrest/Pricer/releases/latest) page
2. Run the program and open settings
3. Pick a league and source to your liking and hit download
4. Choose whatever settings (explained below)
5. Click Apply
6. Click Run
7. Right click (or Ctrl+C) an item

## Settings explained (as of v1.0.14)
* `Show overlay` Displays a small TradeMacro-like box under the cursor that contains the price instead of pasting it (can be closed by moving the cursor away from the box)
* `Send note` copies a buyout note (eg. `~b/o 5 chaos`) to your clipboard and pastes it into the game's note field
* `Send enter` automatically presses enter after pasting the note, applying the price instantly
* `Paste delay` delay in milliseconds between right clicking and pasting the buyout note. Required as the game's note field in premium stash tabs takes some time to open. If the buyout notes are not being pasted, try incrementing this value by 50 until you see a change.
* `Lower price by #%` takes whatever price the item usually goes for and reduces its price by that ppercentage
* `PoePrices Fallback` allows normal/magic/rare items to be priced with the help of [poeprices](https://www.poeprices.info/), the same way TradeMacro does it
* `Run on right click` does what it says. When this is disabled, the program can be used with `Ctrl+C`

## What can it price
Armours, weapons, accessories, flasks, 6-links, 5-links, jewels, gems, divination cards, maps, currency, prophecies, essences, normal, magic and rare items. In short: if it's an item, this program can find a price for it.

## But I have TradeMacro
Yeah and it's slow as s#!t.
You hover over the item, click the hotkey, wait a bunch for it to load and then get a list of the cheapest prices.
Mind you, those might very well be pricefixers.
Not with this program though.
The prices are accurate and represent the average what other people have listed over a certain period of time.
Alls you gotta do is right click and you got your price set!

## Where are the prices from
Three main sources: [poe-stats.com](http://poe-stats.com), [poe.ninja](http://poe.ninja) and [poeprices](https://www.poeprices.info/).

## Words of warning
* The prices might not be always 100% accurate (this is especially evident in sparsely found items, i.e league-specific uniques or certain gems)
* PriceBoxes probably don't work well with full-screen mode
* Prices in hardcore are generally not accurate as nobody plays there
* There might be some issues running this program alongside TradeMacro or similar applications as they both need to access the clipboard
