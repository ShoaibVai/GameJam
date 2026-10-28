using System.Collections.Generic;
using UnityEngine;

namespace OMTK
{
    public static class OmtkArt
    {
        private const float UnitsPerPixel = 2f;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Sprite shieldFull;
        private static Sprite shieldEmpty;
        private static Sprite pipFull;
        private static Sprite pipEmpty;

        public static Sprite Load(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (Cache.TryGetValue(name, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>("UI/" + name);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), UnitsPerPixel);
            sprite.name = name;
            Cache[name] = sprite;
            return sprite;
        }

        public static Sprite ShieldFull(bool available)
        {
            if (shieldFull == null && shieldEmpty == null)
            {
                shieldFull = Load("shield_full");
                shieldEmpty = Load("shield_empty");
            }

            if (!available)
            {
                return shieldEmpty;
            }

            return shieldFull != null ? shieldFull : shieldEmpty;
        }

        public static Sprite Pip(bool filled)
        {
            if (pipFull == null && pipEmpty == null)
            {
                pipFull = Load("pip_full");
                pipEmpty = Load("pip_empty");
            }

            if (!filled)
            {
                return pipEmpty;
            }

            return pipFull != null ? pipFull : pipEmpty;
        }

        public static bool HasShields
        {
            get { return Load("shield_full") != null && Load("shield_empty") != null; }
        }

        public static bool HasPips
        {
            get { return Load("pip_full") != null && Load("pip_empty") != null; }
        }
    }
}
