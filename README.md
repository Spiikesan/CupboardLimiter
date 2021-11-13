## Features

* Set a maximum cupboard limit per group with permissions
* Notify in console if player has too much cupboard already
* Notifies the player he has reached the limit
* Notifies the player remaining tc's after 1st tc placement
* Option to send a message to a discord channel when a player tries to place more tc's then allowed
* in-game admin command to retrieve users TC count and map tile position of each.
* Customizable chat icon and prefix for messages.
* Dynamic limit system, with one permission for each limits defined in the configuration. If multiple permissions are granted to one player, only the maximum limit is taken.
* VIP limit will be taken if it's granted, over any other perm, even higher.

## Permissions

- `cupboardlimiter.bypass` -- Gives No limits on TC placing
- `cupboardlimiter.vip` -- Sets the cupboard limit on player with Vip settings
- `cupboardlimiter.admin` -- Permit the use of commands
- `cupboardlimiter.limit_X` -- where X is the index in the array of "Limit Others" limits in the configuration, begining at 1.

## Commands

- `clinspect <partialUserNameOrId>` -- Retrieve the number and position of all TCs for a specific player using it's name (or only a part of it) or it's steam userID. Need the admin permission.

## Suggestions

The roadmap of this plugin depends on your suggestions ! (I'll try to add your features as quickly as possible, if they are relevant.)
 
 - Clan based limit
 - Team-based limits.
 - Per-zone limit

## Configuration


```json
{
  "Max amount of TC(s) to place": {
    "Limit Default": 1,
    "Limit Vip": 3,
	"Limit Others": []
  },
  "Discord Notification": {
    "Discord Webhook URL": ""
  },
  "Chat Settings": {
    "Prefix": "[Cupboard Limiter] :",
	"Icon's SteamId": 76561198049668039
  }
}
```

## Localization

Warning: You NEED to delete the lang file prior to update the plugin, or to modify it yourself to match the current placeholder system.

```json
{
  "MaxLimitDefault": "You have reached the Default maximum cupboard limit of {0}", //{0} => TC count
  "MaxLimitVip": "You have reached the Vip maximum cupboard limit of {0}", //{0} => TC count
  "Remaining": "Amount of TC's remaining = {0}", //{0} => TC count
  "NoPermission": "You don't have the permission.",
  "cInspect": "The user {0} have {1} TCs.", //{0} => userName, {1} => TC count
  "cInspectUsage": "Usage: /{0} <userNameOrId>", //{0} => Command name
  "cInspectNotFound": "Error: User not found"
}
```

Current native localizations are English (en) and French (fr).

## Credits 
- **BuzZ** The original author
- **Spiikesan** Maintainer (> v1.3.0)