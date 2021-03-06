using UnityEngine;

namespace Rebar.Unity
{
    public static class Vector2IntExtensions
    {
        /// <summary>
        /// Given the current Vector (x, y), it returns (x, 0)
        /// </summary>
        public static Vector2Int x(this Vector2Int vector) => new Vector2Int(vector.x, 0);

        /// <summary>
        /// Given the current Vector (x, y), it returns (0, y)
        /// </summary>
        public static Vector2Int y(this Vector2Int vector) => new Vector2Int(0, vector.y);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (x, x)
        /// </summary>
        public static Vector2Int xx(this Vector2Int vector) => new Vector2Int(vector.x, vector.x);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (y, y)
        /// </summary>
        public static Vector2Int yy(this Vector2Int vector) => new Vector2Int(vector.y, vector.y);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (y, x)
        /// </summary>
        public static Vector2Int yx(this Vector2Int vector) => new Vector2Int(vector.y, vector.x);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (x, x, x)
        /// </summary>
        public static Vector3Int xxx(this Vector2Int vector) => new Vector3Int(vector.x, vector.x, vector.x);

        /// <summary>
        /// Sizzling operator. Give the Vector (x, y), it returns (y, y, y)
        /// </summary>
        public static Vector3Int yyy(this Vector2Int vector) => new Vector3Int(vector.y, vector.y, vector.y);

        /// <summary>
        /// Given the Vector (x, y) and a value x', it returns (x', y)
        /// </summary>
        public static Vector2Int WithX(this Vector2Int vector, int x) => new Vector2Int(x, vector.y);

        /// <summary>
        /// Given the Vector (x, y) and a value y', it returns (x, y')
        /// </summary>
        public static Vector2Int WithY(this Vector2Int vector, int y) => new Vector2Int(vector.x, y);
    }
}
