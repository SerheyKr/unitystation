using System.Collections;
using System.Collections.Generic;
using Doors;
using HealthV2;
using PathFinding;
using Systems.MobAIs;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mobs.BrainAI
{
	public class BrainMobPathfinderV2 : MonoBehaviour
	{
		private RegisterTile registerTile;
		private Matrix matrix => registerTile.Matrix;
		public MovementSynchronisation Movement { get; private set; }
		public Rotatable Rotatable { get; private set; }

		private LivingHealthMasterBase health;
		[SerializeField] private Mob mobBase;

		[Range(0.01f, 1), FormerlySerializedAs("tickRate")]
		[Tooltip("Delay (in seconds) between mob actions/decisions.")]
		public float TickDelay = 1f;
		private float tickWait;
		private float timeOut;

		private bool movingToTile;
		private bool arrivedAtTile;

		//For OnDrawGizmos
		private List<Node> debugPath = null;
		private Dictionary<Vector2, Color> debugSearch = new Dictionary<Vector2, Color>();


		private Node startNode;
		private Node goalNode;
		private bool pathFound;
		PriorityQueue<Node> frontierNodes;
		private List<Node> exploredNodes = new List<Node>();
		private Dictionary<Vector2Int, Node> allNodes = new Dictionary<Vector2Int, Node>();

		private bool isComplete = false;

		public virtual void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			Rotatable = GetComponent<Rotatable>();
			mobBase = GetComponent<Mob>();
			health = GetComponent<LivingHealthMasterBase>();
			Movement = GetComponent<MovementSynchronisation>();;
		}

		public virtual void OnEnable()
		{
			//only needed for starting via a map scene through the editor:
			if (CustomNetworkManager.Instance == null) return;

			if (CustomNetworkManager.IsServer)
			{
				Movement.OnLocalTileReached.AddListener(OnTileReached);
			}
		}

		public virtual void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				Movement.OnLocalTileReached.RemoveListener(OnTileReached);
			}
		}

		void OnDrawGizmos()
		{
			if (debugPath != null)
			{
				for (var i = 1; i < debugPath.Count; ++i)
				{
					var from = new Vector3(debugPath[i - 1].position.x + 1, debugPath[i - 1].position.y + 1, 0);
					var to = new Vector3(debugPath[i].position.x + 1, debugPath[i].position.y + 1, 0);
					Gizmos.DrawLine(from, to);
				}
			}

			if (debugSearch.Count > 0)
			{
				foreach (KeyValuePair<Vector2, Color> kvp in debugSearch)
				{
					Gizmos.color = kvp.Value;
					Gizmos.DrawCube(transform.parent.TransformPoint((Vector3)kvp.Key), Vector3.one * 0.9f);
				}
			}
		}

		///<summary>
		/// Use local positions related to the matrix the object is on
		///</summary>
		public List<Node> FindNewPath(Vector2Int startPos, Vector2Int targetPos)
		{
			if (startPos == targetPos) return null;

			pathFound = false;

			startNode = new Node
			{
				position = startPos,
				distanceTraveled = 0
			};

			goalNode = new Node
			{
				position = targetPos
			};

			if (Vector2Int.Distance(startNode.position, goalNode.position) < 2f)
			{
				//This is just the next tile over, create a path with start and goal nodes
				return new List<Node>(new Node[] { startNode, goalNode });
			}

			isComplete = false;
			isComplete = false;
			frontierNodes = new PriorityQueue<Node>();
			frontierNodes.Enqueue(startNode);
			exploredNodes.Clear();
			allNodes.Clear();
			allNodes.Add(startNode.position, startNode);
			allNodes.Add(goalNode.position, goalNode);
			return SearchForRoute();
		}

		List<Node> SearchForRoute()
		{
			int timeOut = 0;

			while (!isComplete)
			{
				timeOut++;
				if (timeOut > 200)
				{
					isComplete = true;
					//This could be because you are trying to use a goal node that is inside a wall or the path was blocked
					Logger.Log(
						$"Pathing finding could not find a path where one was expected to be found. StartNode {startNode.position} GoalNode {goalNode.position}",
						Category.Movement);
					return null;
				}

				if (frontierNodes.Count > 0)
				{
					Node currentNode = frontierNodes.Dequeue();

					if (currentNode.neighbors == null)
					{
						FindNeighbours(currentNode);
					}

					if (!exploredNodes.Contains(currentNode))
					{
						exploredNodes.Add(currentNode);
					}

					ExpandFrontierAStar(currentNode);

					if (pathFound)
					{
						isComplete = true;
						List<Node> path = new List<Node>();

						Node nextNode = goalNode;

						do
						{
							path.Add(nextNode);
							nextNode = nextNode.previous;
						} while (nextNode != null);

						path.Reverse();

						return path;
					}
				}
				else
				{
					isComplete = true;
					return null;
				}
			}

			return null;
		}

		void FindNeighbours(Node currentNode)
		{
			Vector2Int startPos = currentNode.position + new Vector2Int(-1, 1);
			List<Node> newNeighbours = new List<Node>(8);

			for (int i = 0; i < 3; i++)
			{
				for (int y = 0; y < 3; y++)
				{
					Vector2Int searchPos = new Vector2Int(startPos.x + i, startPos.y - y);
					if (searchPos == currentNode.position)
					{
						continue;
					}

					if (!allNodes.ContainsKey(searchPos))
					{
						Node newNode = new Node
						{
							position = searchPos
						};
						allNodes.Add(newNode.position, newNode);
						newNeighbours.Add(newNode);
					}
					else
					{
						newNeighbours.Add(allNodes[searchPos]);
					}
				}
			}

			currentNode.neighbors = newNeighbours;
		}

		private void ExpandFrontierAStar(Node node)
		{
			if (node == null)
				return;

			for (int i = 0; i < node.neighbors.Count; i++)
			{
				Node neighbor = node.neighbors[i];


				if (exploredNodes.Contains(neighbor))
				{
					continue;
				}

				RefreshNodeType(neighbor);

				if (node.neighbors[i].nodeType == PathFinding.NodeType.Blocked)
				{
					continue;
				}

				float distanceToNeighbor = GetNodeDistance(node, neighbor);
				float newDistanceTraveled = distanceToNeighbor + node.distanceTraveled + (int)node.nodeType;
				uint newPriority = (uint)(100 * (newDistanceTraveled + GetNodeDistance(neighbor, goalNode)));

				if (neighbor.priority > newPriority)
				{
					neighbor.previous = node;
					neighbor.distanceTraveled = newDistanceTraveled;
					neighbor.priority = newPriority;

					if (node == goalNode || neighbor == goalNode)
					{
						pathFound = true;
						break;
					}
					else
					{
						frontierNodes.Remove(neighbor); // Re-sort if the node existed already.
						frontierNodes.Enqueue(neighbor);
					}
				}
			}
		}

		/// <summary>
		/// Start following a given path
		/// </summary>
		public void FollowPath(List<Node> path)
		{
			if (health.IsDead || health.IsCrit)
			{
				Logger.Log("You are trying to follow a path when living thing is dead or in crit", Category.Movement);
				return;
			}

			//	debugPath = path;
			StartCoroutine(PerformFollowPath(path));
		}

		IEnumerator PerformFollowPath(List<Node> path)
		{
			int node = 1;

			while (node < path.Count)
			{
				yield return WaitFor.EndOfFrame;
				if (health.IsDead)
				{
					yield break;
				}

				if (!movingToTile)
				{
					if (TickDelay.Approx(0) == false)
					{
						yield return WaitFor.Seconds(TickDelay);
					}

					var dir = path[node].position - Vector2Int.RoundToInt(transform.localPosition);
					if (!registerTile.Matrix.IsPassableAtOneMatrixOneTile(registerTile.LocalPositionServer + (Vector3Int)dir, true, context: gameObject))
					{
						var dC = registerTile.Matrix.GetFirst<DoorController>(
							registerTile.LocalPositionServer + (Vector3Int)dir, true);
						if (dC != null)
						{
							dC.MobTryOpen(gameObject);
							yield return WaitFor.Seconds(1f);
						}
						else
						{
							ResetMovingValues();
							FollowCompleted();
							Logger.Log("Path following timed out. Something must be in the way", Category.Movement);
							yield break;
						}
					}

					if (Rotatable != null)
					{
						Rotatable.SetFaceDirectionLocalVector(dir);
					}

					Movement.TryTilePush(dir, null);
					movingToTile = true;
				}
				else
				{
					if (arrivedAtTile)
					{
						movingToTile = false;
						arrivedAtTile = false;
						timeOut = 0f;
						node++;
					}
					else
					{
						//Mob has 5 seconds to get to the next tile
						//or the AI should do something else
						timeOut += MobController.UpdateTimeInterval;
						if (timeOut > 5f)
						{
							ResetMovingValues();
							Logger.Log("Path following timed out. Something must be in the way", Category.Movement);
							FollowCompleted();
							yield break;
						}
					}
				}

				yield return WaitFor.EndOfFrame;
			}

			yield return WaitFor.EndOfFrame;

			ResetMovingValues();
			FollowCompleted();
		}

		private void ResetMovingValues()
		{
			movingToTile = false;
			arrivedAtTile = false;
			timeOut = 0f;
		}

		/// <summary>
		/// This is called whenever a follow path has been completed
		/// It is also called on a path follow timeout or if something has
		/// blocked the path and the NPC cannot go any further
		/// </summary>
		protected virtual void FollowCompleted()
		{
		}

		/// <summary>
		/// This method is called if something has moved into the path
		/// and has been blocking the agents path forward for
		/// more then 5 seconds. PathFinder will go back to idle when
		/// this is called
		/// </summary>
		protected virtual void PathMoveTimedOut()
		{
		}

		protected virtual void OnTileReached(Vector3Int oldLocalPos, Vector3Int newLocalPos)
		{
			if (movingToTile && !arrivedAtTile)
			{
				arrivedAtTile = true;
			}
		}

		private void RefreshNodeType(Node node)
		{
			Vector3Int checkPos = new Vector3Int(node.position.x,
				node.position.y, 0);

			if (checkPos == registerTile.LocalPositionServer)
			{
				node.nodeType = PathFinding.NodeType.Open;
				return;
			}

			if (matrix.IsPassableAtOneMatrixOneTile(checkPos, true, context: gameObject))
			{
				node.nodeType = PathFinding.NodeType.Open;
				return;
			}
			else
			{
				var getDoor = matrix.GetFirst<DoorController>(checkPos, true);
				if (getDoor)
				{
					if (!getDoor.AccessRestrictions || (int)getDoor.AccessRestrictions.restriction == 0)
					{
						node.nodeType = PathFinding.NodeType.Open;
					}
					else
					{
						node.nodeType = PathFinding.NodeType.Blocked;
					}
				}
				else
				{
					node.nodeType = PathFinding.NodeType.Blocked;
				}
			}
		}

		private static float GetNodeDistance(Node source, Node target)
		{
			return Vector2.Distance(source.position, target.position);
		}
	}
}
