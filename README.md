# Path Tools

A Unity package that allows you to create and manipulate paths for game objects to follow. This tool is simple to use, customizable, and supports baking paths for improved performance.

![Path Editor Preview](https://github.com/romifauzi/PathTools/raw/main/PathTools.gif)

## Installation

1. Open Unity and go to the **Package Manager** window.
2. Press the `+` button in the top left corner.
3. Choose **"Add package from Git URL..."**
4. Paste the following URL:`https://github.com/romifauzi/PathTools.git?path=/Assets/PathTools`
5. The package will be added to your project. You can also check the included samples for example scenes and usage.

## How to Use

1. **Add the Path Script:**
- Add the `PathScript` component to any GameObject.
- Start adding nodes to create your path, then move and adjust the nodes and their handles as needed.

2. **Make an Object Follow the Path:**
- Add the `MoveAlongPath` component to any GameObject that you want to move along the path.
- Assign the GameObject with the `PathScript` component to the `path` field in the `MoveAlongPath` component.
- Adjust properties in `MoveAlongPath` such as speed, looping, etc., to suit your needs.
- Done! Your GameObject will now follow the path.

## How to Bake Path

Baking the path can improve performance by precomputing the path data.

1. **Add the BakedPath Component:**
- Add the `BakedPath` component to a different GameObject (separate from the one with `PathScript`).

2. **Bake the Path:**
- In the inspector of the GameObject with the `PathScript` component, drag the GameObject with the `BakedPath` component to the **"Baked Path"** field.
- Press the **BAKE** button.

Now, the `MoveAlongPath` component can use the baked path for better performance by assigning the `BakedPath` GameObject.

## Customizing Path Movement

If you want to customize or create your own path-following behavior, feel free to explore the `MoveAlongPath` script. It offers a straightforward way to work with the `PathBase` object, and you should be able to easily adapt it to fit your own requirements.

---

Enjoy using **Path Tools** to create smooth and dynamic paths in your Unity projects and please report any bug, Thanks!
