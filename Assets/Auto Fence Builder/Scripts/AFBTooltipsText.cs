using UnityEngine;
using System.Collections;

public static class AFBTooltipsText {

	public const string fenceHeight = "This is global fence height and scales the height of everything. If you only want to change post height, use Post Size : y instead."; 
	//public const string useRailsB = "Enable this to add a 2nd independent set of rails to the fence.";
	public const string overlapAtCorners = "When rails meet at a corner, this will stretch and move them a little to overlap them and fill any gaps. Especially useful when you do not have any posts.";
	public const string numStackedRailsA = "The number of rails stacked vertically within each section of fence.";
	//public const string railASpread = "The distance between the bottom and top rails.";
	public const string allowGaps = "With this enabled Control-Right-Click to define a section as a gap.  (Blue lines can optionally be shown to indicate that this gap is part of the fence)";
	public const string mainPostSizeBoost = "Boosts the size of the main (user click-point) posts, not the interpolated posts. Good for extra variation. Default = (1,1,1)";
	public const string rotateY = "Rotates every other Rail on the Y axis to help disguise texture repeats.";
	public const string subsGroundBurial = "When subposts are forced to stretch down to the ground contour, this determines how far in the ground they are buried.";
	public const string lerpPostRotationAtCorners = "The Post (or Extra) will be the average direction of the incoming and outgoing rails. Switch off to have it facing exactly the direction of the next rail.";
	public const string relativeMovement = "Positions the Extra between posts. 0(Previous Post) to 1(Next Pos) ";
	public const string relativeScaling = "Scales the Extra's length by the distance between posts. Useful for making extras stretch exactly from one post to another";
	public const string raiseExtraByPostHeight = "Sets the Extra's height to the top of the post, plus it's own offset";
	public const string autoRotateExtra = "Extras will rotate to follow the direction of the fence.";
	public const string interpolate = "Created extra posts and fence/wall sections between your click-points. Default is ON.";
	public const string smooth = "Created extra posts and fence/wall sections between your click-points, and modifies their positions to create curves.";
	public const string removeIfLessThanAngle = "Smoothing can create many extra sections. If you don't need so many on straight sections, use this.";
	public const string stripTooClose = "Smoothing may place posts very close together to smooth sharp corners. Use this to increase the minimum distance";
	public const string addColliders = "Adds one box collider between each pair of posts (not subposts)";
	public const string globalLift = "Lifts everything in the fence off the ground. Primarily intended for creating stacks when cloning fences";

	}
