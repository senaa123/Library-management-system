using LibraryM.Application.Books;
using LibraryM.Application.Books.Models;
using LibraryM.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryM.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var books = await _bookService.GetBooksAsync(category, cancellationToken);
        return Ok(books);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDto>> GetBook(int id, CancellationToken cancellationToken)
    {
        var result = await _bookService.GetBookByIdAsync(id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : NotFound();
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<BookDto>> PostBook([FromBody] CreateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(new { message = result.Message });
        }

        return CreatedAtAction(nameof(GetBook), new { id = result.Value.Id }, result.Value);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutBook(int id, [FromBody] UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookService.UpdateAsync(id, request, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.FailureType == FailureType.NotFound
            ? NotFound()
            : BadRequest(new { message = result.Message });
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBook(int id, CancellationToken cancellationToken)
    {
        var result = await _bookService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound();
    }
}
