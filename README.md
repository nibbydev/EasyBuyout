# EasyBuyout for Path of Exile

![EasyBuyout](screenshots/action.gif)

## What does it do
Set a buyout notes on items with right click based on prices from [poe.ninja](http://poe.ninja). This handy program instantly outputs a buyout note containing the average price, allowing you to price hundreds of items without the tedious work of looking them up individually.

## Who is it for
Be you a labrunner or an Uber Atziri farmer, who has dozens upon dozens of gems or maps that need prices or maybe just your average player who wants to actually enjoy the game instead of playing Shoppe Keep all day long. Well, then this program is perfect for you.

## Settings explained (as of v1.0.14)
* `Show overlay` Displays a small TradeMacro-like box under the cursor that contains the price instead of pasting it (can be closed by moving the cursor away from the box)
* `Send note` copies a buyout note (eg. `~b/o 5 chaos`) to your clipboard and pastes it into the game's note field
* `Send enter` automatically presses enter after pasting the note, applying the price instantly
* `Paste delay` delay in milliseconds between clicking and pasting the buyout note. Required as the premium stash's price menu takes some time to open. If the buyout notes are not being pasted, try incrementing this value by 50 until you see a change.
* `Lower price by #%` takes whatever price the item usually goes for and reduces its price by that percentage
* `Live update` Automatically downloads fresh prices every 10 minutes
* `Price precision` Rounds prices. For example, precision 2 would mean `5.43262` -> `5.43`

## What can it price
Armours, weapons, accessories, flasks, 6-links, 5-links, jewels, gems, divination cards, maps, currency, prophecies, essences, you name it. In short: if it's an item, this program can find a price for it.

## Words of warning
* The prices might not be always 100% accurate because they are from poe.ninja
* PriceBoxes probably don't work well with full-screen mode
* Prices in hardcore are generally not accurate as nobody plays there
* There might be some issues running this program alongside TradeMacro or similar applications as they both need to access the clipboard
