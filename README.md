# cozyhome-decoupling

This is going to be the hub for my mini-packages that i'll be using for my games. Instead of coding every single thing in my games over and over again, I think it'd be best to develop templates to re-use for later projects. This project currently includes:

1. Actor Package (60% Complete as of 1/30/2021) - The ability to use three different primitives for sliding and flying around the scene. This project is akin to a character controller and is free for anybody else to use. It's currently not finished, but when it is I hope it will be incredibly useful. Unity's base CharacterController package isn't very flexible, and I am designing this to circumvent that.

2. Archetype Package (80% Complete as of 1/30/2021) - The ability to trace and overlap three different primitives (used primarily in the Actor package) to set up the annoying boilerplate code required for collision detection/resolution using Unity's NonAlloc variants of the Cast functions.

3. Singleton Package - (Complete as of 1/30/2021) The ability to design any singleton class at compile time and not have to rewrite annoying boilerplate code for it. Singletons aren't necessarily recommended for usage in games, but I find it integrates quite well with the Systems package.

4. Systems Package - (90% Complete as of 1/30/2021) The ability to design "systems" akin to the ECS-paradigm found in Unity's new DOTS. I felt the paradigm was quite simple and nice to use, so I converted it to Mono. It doesn't have any advantage in terms of performance though, its just for development. Feel free to fuck around with it if you'd like.

5. Vectors Package - (Complete as of 1/30/2021) A bunch of methods to help with simple linear algebra problems found in collision resolution with traces/sliding algos written in the Actor package.
