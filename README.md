## Features

* Set a maximum cupboard limit per player with permissions
* Notify in console if player has too much cupboard already
* Notifies the player he has reached the limit
* Notifies the player remaining tc's after 1st tc placement
* Option to send a message to a discord channel when a player tries to place more tc's then allowed

## Performance Update
Instead of triggering on each placement and checking each prefab placement it will now do the following.

* On startup, all tool cupboards instances IDs are added to a list, linked to players ids
* On destroy removes the id from the toolcupboard list of the player.
* On new placement ads the id.

In this way the trigger for *OnEntitySpawned* will search in the player's toolcupboard list instead of
every item on the server enhancing the performance greatly.

## Permissions

- `cupboardlimiter.bypass` -- Gives No limits on TC placing
- `cupboardlimiter.default` -- Sets the cupboard limit on player with default settings
- `cupboardlimiter.vip` -- Sets the cupboard limit on player with Vip settings

## Configuration

```json
{
  "Max amount of TC(s) to place": {
    "Limit Default": 1,
    "Limit Vip": 3
  },
  "Discord Notification": {
    "Discord Webhook URL": ""
  }
}
```

## Localization

```json
{
  "MaxLimitDefault": "You have reached the Default maximum cupboard limit of {0}",
  "MaxLimitVip": "You have reached the Vip maximum cupboard limit of {0}",
  "Remaining": "Amount of TC's remaining = {0}"
}
```

The `{0}` placeholder will be replaced by the amount of TC.

## Credits 
- **BuzZ** The original author
- **Spiikesan** Maintainer (> v1.3.0)