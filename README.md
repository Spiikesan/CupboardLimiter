# Update Information

**_BREAKING CHANGE_ for version 1.6.1 when upgrading: The Team Limit is now global per team and not per member. The "Global Team Limit" is true by default in the config because this behaviour is more likely what server owners expect**

## Features

* Set a maximum cupboard limit per group with permissions
* Notify in console if player has too much cupboard already
* Notifies the player if he has reached the limit
* Notifies the player remaining tc's after each tc placement
* Option to send a message to a discord channel when a player tries to place more tc's than allowed
* in-game admin command to retrieve users TC count and map tile position of each.
* Customizable chat icon and prefix for messages.
* Dynamic limit system, with one permission for each limits defined in the configuration. If multiple permissions are granted to one player, only the maximum limit is applied.
* VIP limit will be taken if it's granted, over any other perm (even higher).
* Team based limits, with configurable limits for each count of members

## Permissions

- `cupboardlimiter.bypass` -- Gives No limits on TC placing
- `cupboardlimiter.vip` -- Sets the cupboard limit on player with Vip settings
- `cupboardlimiter.admin` -- Permit the use of commands
- `cupboardlimiter.limit_X` -- where X is the index in the array of "Limit Others" limits in the configuration, begining at 1.

## Commands

- `clinspect <partialUserNameOrId>` -- Retrieve the number and position of all TCs for a specific player using it's name (or only a part of it) or it's steam userID. Need the admin permission.

## Roadmap / Suggestions

The roadmap of this plugin depends on your suggestions ! (I'll try to add your features as quickly as possible, if they are relevant.)
 - Send a message to a player if the limit is overpassed the limit of TCs to tell him to remove the difference. If nothing is done after a configurable timer, all TCs bases will decay (as if there was no resources).

## Configuration

Here is the description for an example configuration (not the default one) :
 - The default limit is 5 TC.
 - If the player is in a team with at least 2 and less than 4 members, the limit is 8 TCs for all the team. If there is at least 4 members, the limit is 16 for all the team. If the "Global Team Limit" were false, those limits would be per player.
 - Else If the player has the VIP permission, he will have a limit of 20 TC.
 - Else if the player have any of the limit_1, limit_2 or limit_3 permission, only the granted perm with the maximum amount will be applied (Can be less than the Default one **only** if the "Limit Others Can Downgrade Default" setting is **true**).

Corresponding to the following JSON file:
```json
{
  "Max amount of TC(s) to place": {
    "Limit Default": 5,
    "Limit Vip": 20,
	"Limit Others": [2, 10, 8],
	"Limit Others Can Downgrade Default": true,
	"Global Team Limit": true,
	"Limits In Team" : {
	  {"2", 8},
	  {"4", 16}
	},
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
  "cInspectNotFound": "Error: User not found",
  "TeamOvercount": "You cannot invite this player right now, he have {0} TC too many." // {0} => too many TC amount
}
```

Current native localizations are English (en) and French (fr).

## Credits 
- **BuzZ** The original author
- **Spiikesan** Maintainer (> v1.3.0)
