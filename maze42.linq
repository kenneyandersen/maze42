<Query Kind="Program">
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
</Query>


void Main()
{
	var start = new Coord(0, 0, 0);
	var end = new Coord(9, 9, 1); // husk at coordinater er 0-akselængde-1
	var size = new Size(10, 10, 2);
	var preventMultiLevelStairs = true;
	
	var maze = new MazeGenerator(size, preventMultiLevelStairs: preventMultiLevelStairs).GenerateMaze(start, end);
	if (!maze.End.Visited) {
		"Did not reach end".Dump();
	} 
	var viz = new MazeViz();

	var vizpng = new GraphPngViz(maze);
	var folder = new DirectoryInfo($@"c:\map\map_{DateTime.Now:yyyyMMdd-HHmmss}");
	vizpng.Viz(folder, "map", drawExit: false);
	vizpng.Viz(folder, "solution", drawExit: true);
}

class MazeGenerator
{
	private Random rnd = new Random();
	private Size size;
	private bool preventMultiLevelStairs;
	
		
	public MazeGenerator(Size size, bool preventMultiLevelStairs){
		this.size = size;
		this.preventMultiLevelStairs = preventMultiLevelStairs;
	}
	
	public Maze GenerateMaze(Coord start, Coord end)
	{
		var mz = new Maze(size, start, end);
		foreach (Vertex v in mz.Vertices.Values)
		{
			var neighBoursCoords = Move.GetMoves().Select(m => v.Coord.Offset(m)).Where(m => mz.IsInBounds(m.coord)).ToList();
			neighBoursCoords.ForEach(coord =>
			{
				if (mz.Vertices.TryGetValue(coord.coord, out var nVx)) {
				var oppositeDirection = coord.move.OppositeDirection;
					var wall = nVx.Walls[oppositeDirection];
					if (wall is null || wall.dummy) {
						wall = new Wall(nVx, v, rnd.Next(), false);
						nVx.Walls[oppositeDirection] = wall;
					} 
					v.Walls[coord.move.direction] = wall;
				}
			});
		}
		FillMaze(mz);
		FindExit(mz.Start, 0, null, mz.End.Coord); 
		return mz;
	}
	
	bool FindExit(Vertex node, int depth, Vertex lastNode, Coord finishCoord) {
		bool foundExit = false;
		node.Depth = depth;
		foreach(var wall in node.Walls.Where(w=>!w.dummy && w.Open)){
			var nextNode = wall.OppositeVertex(node);
			if (lastNode is null || nextNode.Coord != lastNode.Coord) {
				foundExit |= FindExit(nextNode, depth+1, node, finishCoord);	
			}
		}
		foundExit |= node.Coord == finishCoord;
		if (foundExit) { 
			node.OnExitPath = true;
			
		}
		return foundExit;
	}
	
	void FillMaze(Maze maze) {
        Dictionary<Coord, Vertex> nodes = maze.Vertices.ToDictionary(v => v.Key, v => v.Value); 
        var node = nodes.Randomize().First().Value;
		SortedDictionary<(int, Guid), (Wall wall, Vertex nextNode)> edges = new();
		GetEdgesFromNode(node).ForEach(e => edges.Add((e.wall.weight, Guid.NewGuid()), e));
		node.Visited = true;

		while (edges.Any())
		{
			var edgeKeyVal = edges.First();
			var edge = edgeKeyVal.Value;
			edges.Remove(edgeKeyVal.Key);
			if (edge.nextNode.Visited)
			{
				// opposite node allready visited, skip it
				continue;
			}
			if (preventMultiLevelStairs)
			{
				var lastNode = edge.wall.OppositeVertex(edge.nextNode);
				var nextNode = edge.nextNode;
				if (lastNode.Coord.z < nextNode.Coord.z)
				{
					// lastnode below nextnode
					if (lastNode.Walls[4].Open) {
						continue; // prevent
					}
				}
				if (lastNode.Coord.z > nextNode.Coord.z) {
					// lastnode above 
					if (lastNode.Walls[5].Open) {
						continue; // prevent
					}
				}
			//	continue;
			}
			var newNode = edge.nextNode;
			newNode.Visited = true;
			edge.wall.Open = true;
			GetEdgesFromNode(newNode).ForEach(e => edges.Add((e.wall.weight, Guid.NewGuid()), e));
		}		
	}
	
	bool TryPullNextEdge(List<(Wall wall, Vertex nextNode)> edges, out (Wall wall, Vertex nextNode) result){
		var edgesSorted = edges.OrderBy(e => e.wall.weight);
		if (edgesSorted.Any()) {
			result = edgesSorted.First();
			return true;
		} else {
			result = default;
			return false;
		}
	}

	List<(Wall wall, Vertex nextNode)> GetEdgesFromNode(Vertex node)
	{
		return (from wall in node.Walls
				where !wall.dummy
				let v2 = wall.OppositeVertex(node)
				where v2 is not null && !v2.Visited
				select (wall, v2)
		).ToList();
	}
}

public record Wall(Vertex v1, Vertex v2, int weight, bool dummy)
{
	public bool Open {get;set;} = false;
	
	public Vertex OppositeVertex(Vertex v) {
		return v1 == v ? v2 : v1;
	}
};

public class Vertex
{
	public Coord Coord { get; set; }
	public Guid guid { get; } = Guid.NewGuid();
	public bool Visited {get;set;} = false;
	public bool IsEnd {get;set;}
	public bool IsStart { get; set; }
	public int Depth {get;set;}
	public Vertex LastVertex {get;set;}
	public Move LastMove {get;set;}
	public bool OnExitPath {get;set;} = false;
	public Wall[] Walls {get;} = new Wall[6];
	
	public Vertex() {
		for(int i = 0; i<Walls.Length; i++) {
			Walls[i] = new Wall(null, null, 0, true);
		}
	}
}

public record Coord{
	public int x { get; private set;}
	public int y { get; private set;}
	public int z { get; private set;}
	
	public Coord(int x, int y, int z) {
		this.x = x;
		this.y = y;
		this.z = z;
	}
	public Coord() {}
	
	public (Move move, Coord coord) Offset(Move move)
	{
		return (move, new Coord
		{
			x = this.x + move.x,
			y = this.y + move.y,
			z = this.z + move.z
		});
	}
}
public record Move(int x, int y, int z, string id, int direction)
{
	public static List<Move> GetMoves(){
		return new List<Move>{
			new Move(-1,0,0, "L",0),
			new Move(1,0,0, "R",1),
			new Move(0,1,0, "F", 3),
			new Move(0,-1,0, "B", 2),
			new Move(0,0,1, "U", 5),
			new Move(0,0,-1, "D", 4)
		};
	}
	
	public bool Vertical => z != 0;
	public int OppositeDirection =  direction+1-(direction%2*2); // switch direction to opposite position
	
	public string InverseId => id switch {
		"L" => "R",
		"R" => "L",
		"F" => "B",
		"B" => "F",
		"U" => "D",
		"D" => "U",
		_ => ""
	};
}
;

record Size(int width, int height, int depth);

public static class Extensions
{

	private static Random rnd = new Random();
	public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
	{
		//Random rnd = new Random();
		return source.OrderBy<T, int>((item) => rnd.Next());
	}
}

class Maze {
	public Dictionary<Coord, Vertex> Vertices { get; private set; }
	public Size Size { get; private set;}
	public Vertex Start {get;private set;}
	public Vertex End {get;private set;}

	public Maze(Size size, Coord start, Coord end)
	{
		Size = size;
		if (!IsInBounds(start) || !IsInBounds(end))
		{
			throw new ArgumentException("Start or end is outside the bounds");
		}
		Vertices = new();
		// fill with vertizes
		for (int x = 0; x < size.width; x++)
		{
			for (int y = 0; y < size.height; y++)
			{
				for (int z = 0; z < size.depth; z++)
				{
					var coord = new Coord(x, y, z);
					Vertices[coord] = new Vertex { Coord = coord, IsStart = coord == start, IsEnd = coord == end };
				}
			}
		}
		Start = Vertices[start];
		End = Vertices[end];
	}

	public bool IsInBounds(Coord coord)
	{
		return coord.x >= 0 && coord.y >= 0 && coord.z >= 0 && Size.width > coord.x && Size.height > coord.y && Size.depth > coord.z;
	}

	//public List<Wall> FindInvisitedWalls(Vertex vertex) {
	//	return FindUnvisitedNeightbour(vertex).Select(n => new Wall(vertex, n.move, n.vertex)).ToList();	
	//}
	//
	public List<(Move move, Vertex vertex)> FindUnvisitedNeightbour(Vertex vertex)
	{
		var candidates = Move.GetMoves().Select(m => vertex.Coord.Offset(m)).ToList();
		return candidates.Select(c =>
		{
			var exists = Vertices.TryGetValue(c.Item2, out var vertex);
			return (c.Item1, vertex);
		}).Where(v => v.vertex is not null && !v.vertex.Visited).ToList();
	}

}

class MazeViz
{
	private bool simpleViz;
	private bool showDepth;
	private bool showPath;
	
	public void Viz(Maze maze, bool simpleViz, bool showDepth, bool showPath)
	{
		this.showPath = showPath;
		this.simpleViz = simpleViz;
		this.showDepth = showDepth;
		var size = maze.Size;
		string[][,] mazeChars = new string[size.depth][,];
		Enumerable.Range(0, size.depth).ToList().ForEach(d =>
		{
			var lvl = new string[size.height * 3 - size.height + 1, size.width * 3 - size.width + 1];
			mazeChars[d] = lvl;
			// start by filling everything
			for (int x = 0; x < lvl.GetLength(1); x++)
			{
				for (int y = 0; y < lvl.GetLength(0); y++)
				{
					lvl[y, x] = "█";
				}
			}
		});
		var path = FindPath(maze);
		maze.Vertices.ToList().ForEach(m =>
		{
			var coord = m.Key;
			var lvl = mazeChars[coord.z];
			var vertex = m.Value;
			var centerX = coord.x * 2 + 1;
			var centerY = coord.y * 2 + 1;
			lvl[centerY, centerX] = showDepth ? vertex.Depth.ToString() : "";
			lvl[centerY, centerX] += simpleViz ? "" : vertex.LastMove?.InverseId + " / ";
			lvl[centerY, centerX] += (vertex.IsStart ? "S" : (vertex.IsEnd ? "E" : ""));
			lvl[centerY, centerX] += showPath && path.TryGetValue(vertex, out var val) ? $"({val})" : "";
			var walls = vertex.Walls;
			if (walls[4]?.Open == false)
			{
				lvl[centerY, centerX] += "↓";
			}
			if (walls[5]?.Open == false)
			{
				lvl[centerY, centerX] += "↑";
			}
			var doorWayChar = "▒";
			if (walls[0]?.Open == false)
			{
				lvl[centerY, centerX - 1] = doorWayChar;
			}
			if (walls[1]?.Open == false)
			{
				lvl[centerY, centerX + 1] = doorWayChar;
			}
			if (walls[2]?.Open == false)
			{
				lvl[centerY - 1, centerX] = doorWayChar;
			}
			if (walls[3]?.Open == false)
			{
				lvl[centerY + 1, centerX] = doorWayChar;
			}
		});
		mazeChars.Select((lvl, ix) => new
		{
			LevelNumber = ix,
			Level = lvl
		}).Dump();
	}
	
	private Dictionary<Vertex, int> FindPath(Maze maze){
		Dictionary<Vertex, int> vertices = new Dictionary<UserQuery.Vertex, int>();
		var vertex = maze.End;
		int num = 0;
		while(vertex is not null) {
			vertices[vertex] = num--;
			vertex = vertex.LastVertex;
		}
		foreach(var key in vertices.Keys) {
			vertices[key] = vertices[key]-num;
		}
		return vertices;
	}
}

class GraphPngViz
{
	Maze maze;
	GraphVizLayoutParams l;

	public GraphPngViz(Maze maze)
	{
		this.maze = maze;
		l = new GraphVizLayoutParams();
	}

	public void Viz(DirectoryInfo dir, string filebase, bool drawExit)
	{
		dir.Create();
		var width = l.WallLength * (maze.Size.width + 2);
		var height = l.WallLength * (maze.Size.height + 2);
		foreach (var depth in Enumerable.Range(0, maze.Size.depth))
		{
			var bmp = new Bitmap(width, height);
			var vertices = maze.Vertices.Values.Where(w => w.Coord.z == depth).ToList();
			DrawMap(vertices, bmp, depth, drawExit);
			bmp.Dump();
			bmp.Save(Path.Combine(dir.FullName, $"{filebase}_{depth}.png"), ImageFormat.Png);
		}
	}

	private void DrawMap(List<Vertex> vertices, Bitmap bmp, int level, bool drawExit)
	{
		var g = Graphics.FromImage(bmp);
		g.FillRectangle(l.backgroundBrush, new Rectangle(new Point(0, 0), bmp.Size));
		g.DrawString($"Level: {level + 1 }", l.levelFont, l.levelFontBrush, new PointF(10, 10));
		vertices.ForEach(v => DrawVertex(v, g, bmp, drawExit));
	}

	private void DrawVertex(Vertex v, Graphics g, Bitmap bmp, bool drawExit)
	{
		var coord = v.Coord;
		var vertexRectangle = new Rectangle(
			new((coord.x + 1) * l.WallLength, (coord.y + 1) * l.WallLength),
			new(l.WallLength, l.WallLength)
		);

		var floorBrush = v switch
		{
			{ IsStart: true } => l.floorStartBrush,
			{ IsEnd: true } => l.floorEndBrush,
			{ OnExitPath: true } when drawExit == true => l.exitPathFlootBrush,
			_ => l.floorBrush,
		};

		g.FillRectangle(floorBrush, vertexRectangle);

		var corners = new Dictionary<int, Point> {
			{0, vertexRectangle.Location},
			{1, new Point(vertexRectangle.Right,vertexRectangle.Top) },
			{2, new Point(vertexRectangle.Left, vertexRectangle.Bottom)},
			{3, new Point(vertexRectangle.Right, vertexRectangle.Bottom)}
		};
		
		corners.Values.ToList().ForEach(c =>
		{
			g.FillEllipse(l.cornerBrush, c.X - l.CorderRadius, c.Y - l.CorderRadius, l.CorderRadius * 2, l.CorderRadius * 2);
		});
		var wallPoints = new Dictionary<int, (Point, Point, Pen)> {
			{0, (corners[0], corners[2], new Pen(Color.Green,5))},
			{1, (corners[1], corners[3], new Pen(Color.Pink,5))},
			{2, (corners[0], corners[1], new Pen(Color.Turquoise,5))},
			{3, (corners[2], corners[3], new Pen(Color.Chocolate,5))},
		};
		wallPoints.ToList().ForEach(wp => {
			if (!v.Walls[wp.Key].Open) {
				g.DrawLine(l.wallPen, wp.Value.Item1, wp.Value.Item2);
			}
		});
		var upDownText = v.Walls[4].Open ? "↓" : "";
		upDownText +=  v.Walls[5].Open ? "↑" : ""; 
		var vertexText = v.IsStart ? "S" : (v.IsEnd ? "E" : "");
		vertexText += upDownText;
		g.DrawString(vertexText, l.vTextFont, l.vTextFontBrush, vertexRectangle, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
		g.DrawString($"{{{v.Coord.x},{v.Coord.y}}}", l.coordFont, l.coordFontBrush, vertexRectangle, new StringFormat{Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far });
		g.DrawString(v.Depth.ToString(),l.depthFont, l.depthFontBrush, vertexRectangle, new StringFormat{Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near }); 
	}
}

class GraphVizLayoutParams
{
	public Brush floorBrush = new SolidBrush(Color.White);
	public Brush floorStartBrush = new SolidBrush(Color.LightGreen);
	public Brush floorEndBrush = new SolidBrush(Color.LightPink);
	public Brush exitPathFlootBrush = new SolidBrush(Color.LightBlue);
	public Pen wallPen = new Pen(Color.Black,3);
	public Brush backgroundBrush = new SolidBrush(Color.Beige);
	public Brush cornerBrush = new SolidBrush(Color.Blue);
	
	public Font levelFont = new Font(FontFamily.GenericSerif, 12f);
	public Brush levelFontBrush = new SolidBrush(Color.Black);
	public Font vTextFont = new Font(FontFamily.GenericSerif, 10f);
	public Brush vTextFontBrush = new SolidBrush(Color.Red);
	public Font coordFont = new Font(FontFamily.GenericSerif, 6);
	public Brush coordFontBrush = new SolidBrush(Color.Green);
	public Font depthFont = new Font(FontFamily.GenericSerif, 6);
	public Brush depthFontBrush = new SolidBrush(Color.Black);

	public ushort WallLength = 50;
	public ushort CorderRadius = 5;
}