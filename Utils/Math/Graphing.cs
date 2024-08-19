using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Utils
{
    public static class Graphing
    {
        public struct Link<T>
        {
            public readonly T Vertex;
            public readonly float Distance;

            public Link(T vertex, float distance)
            {
                Vertex = vertex;
                Distance = distance;
            }
        }
        private struct Path<T> : IEnumerable<T>
        {
            public readonly T[] Vertices;
            public readonly float Distance;

            public Path(T vertex)
            {
                Vertices = new T[] { vertex };
                Distance = 0;
            }
            public Path(Path<T> path, Link<T> link)
            {
                Vertices = new T[path.Vertices.Length + 1];
                path.Vertices.CopyTo(Vertices, 0);
                Vertices[Vertices.Length - 1] = link.Vertex;
                Distance = path.Distance + link.Distance;
            }
            public IEnumerator<T> GetEnumerator() => (IEnumerator<T>)Vertices.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Vertices.GetEnumerator();
        }

        public static IEnumerable<T> FindShortestPath<T>(T startVertex, T endVertex, Func<T, IEnumerable<Link<T>>> getLinks)
        {
            var paths = new Dictionary<T, Path<T>>() // Keeps track of the shortest path to the given vertex, including the vertices along the way and the distance.
            {
                { startVertex, new Path<T>(startVertex) }
            };
            var linkQueue = new SortedSet<Link<T>>(Comparer<Link<T>>.Create((a, b) => a.Distance.CompareTo(b.Distance)))
            {
                new Link<T>(startVertex, 0)
            };

            while (linkQueue.Count > 0)
            {
                Link<T> currentLink = linkQueue.Min;
                linkQueue.Remove(currentLink);
                var currentPath = paths[currentLink.Vertex];
                foreach (Link<T> link in getLinks(currentLink.Vertex))
                {
                    if (paths.TryGetValue(link.Vertex, out Path<T> path))
                    {
                        if(link.Distance + currentPath.Distance < path.Distance)
                        {
                            // If the new path is shorter, overwrite the existing one.
                            paths[link.Vertex] = new Path<T>(currentPath, link);
                        }
                        else
                        {
                            // Otherwise, ignore the path
                        }
                    }
                    else
                    {
                        // This is the first time we've seen this vertex, so we only have this path, for now.
                        paths[link.Vertex] = new Path<T>(currentPath, link);
                        // Add it to the queue and record the current path.
                        linkQueue.Add(link);
                    }
                }
            }

            if (paths.TryGetValue(endVertex, out Path<T> bestPath))
            {
                return bestPath.Vertices; // Return the best path for the given end vertex.
            }
            else
            {
                return null; // No path exists.
            }
        }
    }
}
