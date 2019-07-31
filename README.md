# Blavalon

It's Avalon Role Assignments, but this time in Blazor! See also [my Flask implementation](https://github.com/DylanDmitri/spamalot) of the same concept.

The interesting files are the:
  - [single web page](https://github.com/DylanDmitri/Blavalon/blob/master/Blavalon/Pages/Index.razor)
  - [back-end logic](https://github.com/DylanDmitri/Blavalon/blob/master/Blavalon/Data/RoomService.cs)
  
Going to have real push notifications soon. Right now there's a `DylanAsAService` which just re-renders every page twice per second. Doesn't scale well.
