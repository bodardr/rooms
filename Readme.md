Room System, how to setup :

- Ensure you have Cinemachine for this to work.
- Create a new layer called "Room", name is important.
- Modify the Physics2D collision matrix so that the Room layer can only collide with other Rooms (Room -> Room, uncheck the rest)
- Add a Grid GameObject with a child which contains a tilemap. 
	+ Add The RoomManager component inside the grid.
	+ RoomManager requires a RoomTransitionHandler in order to transition properly (surprising I know) so make sure you add it too.
- Your player can now be added.
	+ In order for the player to interact with transitions, etc... Add the RoomCollider along with a 2D Collider of your choice.
	make sure it is on the Room layer.
	
- Enjoy. You should be set. If there's anything, leave me a message : romain.bodard@videotron.ca