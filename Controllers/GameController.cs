
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Battleships_Pantoja_Saavedra.;
using Battleships_Pantoja_Saavedra.Models;

public class GameController : Controller
{
    // Index shows start form
    public IActionResult Index()
    {
        return View();
    }

    // StartGame: create a new player or load existing saved progress (JSON)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StartGame(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) name = "Player";

        // Try to load saved
        var saved = GameStore.Get(name);
        PlayerState state;
        if (saved != null)
        {
            state = saved;
        }
        else
        {
            var player = new Player { Name = name, Level = 1 };
            var board = GenerateBoard(player.Level);
            state = new PlayerState { Player = player, Board = board };
            GameStore.Upsert(state); // persist new record
        }

        ViewBag.PlayerName = state.Player.Name;
        ViewBag.PlayerLevel = state.Player.Level;
        ViewBag.ShipsRemaining = state.Board.Cells.Count(c => c.State == CellState.Ship);

        return View("Board", state.Board);
    }

    // Shoot: receives playerName, x, y; updates JSON and returns result
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Shoot([FromForm] string playerName, [FromForm] int x, [FromForm] int y)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return Json(new { error = "MissingPlayer" });

        var state = GameStore.Get(playerName);
        if (state == null)
            return Json(new { error = "PlayerNotFound" });

        var board = state.Board;
        var cell = board.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
        if (cell == null)
            return Json(new { error = "CellNotFound" });

        if (cell.State == CellState.Hit || cell.State == CellState.Miss)
        {
            int shipsLeftAlready = board.Cells.Count(c => c.State == CellState.Ship);
            return Json(new { result = "Already", shipsRemaining = shipsLeftAlready });
        }

        if (cell.State == CellState.Ship)
            cell.State = CellState.Hit;
        else
            cell.State = CellState.Miss;

        // persist change immediately
        GameStore.Upsert(state);

        int shipsRemaining = board.Cells.Count(c => c.State == CellState.Ship);

        if (shipsRemaining == 0)
        {
            // all destroyed — client will call NextLevel to get new board
            return Json(new { result = cell.State == CellState.Hit ? "Hit" : "Miss", shipsRemaining = 0, nextLevel = state.Player.Level + 1 });
        }

        return Json(new { result = cell.State == CellState.Hit ? "Hit" : "Miss", shipsRemaining });
    }

    // NextLevel: increases player level, generates new board, persists, returns Board view
    [HttpGet]
    public IActionResult NextLevel(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return RedirectToAction("Index");

        var state = GameStore.Get(playerName);
        if (state == null)
            return RedirectToAction("Index");

        state.Player.Level++;
        state.Board = GenerateBoard(state.Player.Level);
        GameStore.Upsert(state);

        ViewBag.PlayerName = state.Player.Name;
        ViewBag.PlayerLevel = state.Player.Level;
        ViewBag.ShipsRemaining = state.Board.Cells.Count(c => c.State == CellState.Ship);

        return View("Board", state.Board);
    }

    // Quit: simply persists (already persisted on each action) and return to Index
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Quit(string playerName)
    {
        // nothing special needed; keep data in JSON; optionally remove if you want to forget progress
        return RedirectToAction("Index");
    }

    // helper: generate board — currently single-cell ships (easy to extend to multi-cell ships)
    private Board GenerateBoard(int level)
    {
        int size = Math.Max(5, 5 + level - 1); // adjust formula as desired
        var board = new Board(size);
        var rnd = new Random();

        int shipsToPlace = Math.Min(size * size / 4, level + 2); // example rule
        int attempts = 0;
        while (board.Cells.Count(c => c.State == CellState.Ship) < shipsToPlace && attempts < shipsToPlace * 10)
        {
            int x = rnd.Next(0, size);
            int y = rnd.Next(0, size);
            var cell = board.Cells.First(c => c.X == x && c.Y == y);
            if (cell.State != CellState.Ship)
                cell.State = CellState.Ship;
            attempts++;
        }

        return board;
    }
}
