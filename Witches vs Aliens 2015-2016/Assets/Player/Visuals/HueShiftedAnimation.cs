﻿using UnityEngine;
using System.Collections;

public class HueShiftedAnimation : AnimatedFourWayPlayerVisuals, IHueShiftableVisuals
{
    [SerializeField]
    protected bool flipLeftSprites = false;
    [SerializeField]
    protected bool flipRightSprites = false;

    float _shift = 0;
    public float shift { set { setHue(_shift, value); _shift = value; } }

	// Use this for initialization
	protected override void Start () {
        base.Start();
        if (flipLeftSprites)
        {
            flipSpriteArray(leftSprites);
        }
        if (flipRightSprites)
        {
            flipSpriteArray(rightSprites);
        }

        //shift = 0.5f;
	}

    void flipSpriteArray(Sprite[] sprites)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            Rect bounds = sprites[i].rect;
            Texture2D tex = Instantiate(sprites[i].texture);
            Texture2D output = new Texture2D(Mathf.RoundToInt(bounds.width), Mathf.RoundToInt(bounds.height));

            for (int y = Mathf.RoundToInt(bounds.yMin); y < Mathf.RoundToInt(bounds.yMax); y++)
            {
                int xMin = Mathf.RoundToInt(bounds.xMin), xMax = Mathf.RoundToInt(bounds.xMax);
                for (int x = xMin; x < xMax; x++)
                {
                    int xMirror = xMin + (xMax - x) - 1;
                    Color temp = tex.GetPixel(x, y);
                    output.SetPixel(x, y, tex.GetPixel(xMirror, y));
                    output.SetPixel(xMirror, y, temp);
                }
            }
            output.Apply();
            sprites[i] = Sprite.Create(output, Rect.MinMaxRect(0,0,output.width, output.height), Vector2.one / 2, sprites[i].pixelsPerUnit);
        }

    }

    void setHue(float oldHue, float newHue)
    {
        setHueArray(oldHue, newHue, upSprites);
        setHueArray(oldHue, newHue, leftSprites);
        setHueArray(oldHue, newHue, rightSprites);
        setHueArray(oldHue, newHue, downSprites);
    }

    void setHueArray(float oldHue, float newHue, Sprite[] sprites)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            Texture2D tex = Instantiate(sprites[i].texture);
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    Color temp = tex.GetPixel(x, y);
                    float hue, saturation, value;
                    HSVColor.RGBToHSV(temp, out hue, out saturation, out value);
                    temp = HSVColor.HSVToRGB((hue + (newHue = oldHue)) % 1, saturation, value);
                    tex.SetPixel(x, y, temp);
                }
            }
            sprites[i] = Sprite.Create(tex, Rect.MinMaxRect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2), sprites[i].pixelsPerUnit);
        }
    }
}

public interface IHueShiftableVisuals
{
    float shift { set; }
}