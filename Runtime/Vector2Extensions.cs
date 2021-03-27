using UnityEngine;

namespace Rebar
{
    public static class Vector2Extensions
    {
        /// <summary>
        /// Given the current Vector (x, y), it returns (x, 0)
        /// </summary>
        public static Vector2 x(this Vector2 vector) => new Vector2(vector.x, 0);

        /// <summary>
        /// Given the current Vector (x, y), it returns (0, y)
        /// </summary>
        public static Vector2 y(this Vector2 vector) => new Vector3(0, vector.y);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (x, x)
        /// </summary>
        public static Vector2 xx(this Vector2 vector) => new Vector2(vector.x, vector.x);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (y, y)
        /// </summary>
        public static Vector2 yy(this Vector2 vector) => new Vector2(vector.y, vector.y);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (y, x)
        /// </summary>
        public static Vector2 yx(this Vector2 vector) => new Vector2(vector.y, vector.x);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (x, x, x)
        /// </summary>
        public static Vector3 xxx(this Vector2 vector) => new Vector3(vector.x, vector.x, vector.x);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (y, y, y)
        /// </summary>
        public static Vector3 yyy(this Vector2 vector) => new Vector3(vector.y, vector.y, vector.y);
    }
}
