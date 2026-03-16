using LibraryM.Application.Books;
using LibraryM.Application.Books.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryM.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BooksController : ApiControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks([FromQuery] BookSearchRequest request, CancellationToken cancellationToken)
    {
        var books = await _bookService.SearchAsync(request, cancellationToken);
        return Ok(books);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDto>> GetBook(int id, CancellationToken cancellationToken)
    {
        var result = await _bookService.GetBookByIdAsync(id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : NotFound();
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPost]
    public async Task<IActionResult> PostBook([FromBody] CreateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ToFailureResult(result);
        }

        return CreatedAtAction(nameof(GetBook), new { id = result.Value.Id }, result.Value);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutBook(int id, [FromBody] UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookService.UpdateAsync(id, request, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return ToFailureResult(result);
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPost("{id:int}/remove")]
    public async Task<IActionResult> RemoveBook(int id, [FromBody] RemoveBookRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookService.RemoveAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToFailureResult(result);
    }
}
