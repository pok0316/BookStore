using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BookStore_API.Contracts;
using BookStore_API.Data;
using BookStore_API.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BookStore_API.Controllers
{
    /// <summary>
    /// Interact with the Books Table
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly IMapper _mapper;

        public BooksController(IBookRepository bookRepository,
            IMapper mapper)
        {
            _bookRepository = bookRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Get all Books
        /// </summary>
        /// <returns>A list of Books</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBooks()
        {
            var location = GetControllerAction();
            try
            {
                Log.Information($"{location}: Attempted Call");
                var books = await _bookRepository.FindAll();
                var response = _mapper.Map<IList<BookDTO>>(books);
                Log.Information($"{location}: Successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalError($"{location}: {ex.Message} - {ex.InnerException}");
            }
        }

        /// <summary>
        /// Get an book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>An Book's record</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBook(int id)
        {
            var location = GetControllerAction();
            try
            {
                Log.Information($"{location}: Attempted Call for id: {id}");
                var book = await _bookRepository.FindById(id);
                if (book == null)
                {
                    Log.Warning($"{location}: Failed to retrieve record with id: {id}");
                    return NotFound();
                }
                var response = _mapper.Map<BookDTO>(book);
                Log.Information($"{location}: Successfully got record with id: {id}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalError($"{location}: {ex.Message} - {ex.InnerException}");
            }
        }

        /// <summary>
        /// Create An Book
        /// </summary>
        /// <param name="book"></param>
        /// <returns>Book Object</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] BookCreateDTO bookDTO)
        {
            var location = GetControllerAction();
            try
            {
                Log.Information($"{location}: Create Attempted");
                if (bookDTO == null)
                {
                    Log.Warning($"{location}: Empty Request was submitted");
                    return BadRequest(ModelState);
                }
                if (!ModelState.IsValid)
                {
                    Log.Warning($"{location}: Data was Incomplete");
                    return BadRequest(ModelState);
                }
                var book = _mapper.Map<Book>(bookDTO);
                var isSuccess = await _bookRepository.Create(book);
                if (!isSuccess)
                    return InternalError($"{location}: creation failed");

                Log.Information($"{location}: Creation was successful");
                return Created("Create", new { book });
            }
            catch (Exception ex)
            {
                return InternalError($"{location}: {ex.Message} - {ex.InnerException}");
            }
        }


        /// <summary>
        /// Update a Book by Id
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] BookUpdateDTO bookDTO)
        {
            var location = GetControllerAction();
            try
            {
                Log.Information($"{location}: Update Attempted on record with id: {id} ");
                if (id < 1 || bookDTO == null || id != bookDTO.Id)
                {
                    Log.Warning($"{location}: Update failed with bad data - id: {id}");
                    return BadRequest();
                }
                var isExists = await _bookRepository.IsExists(id);
                if (!isExists)
                {
                    Log.Warning($"{location}: Failed to retrieve record with id: {id}");
                    return NotFound();
                }
                if (!ModelState.IsValid)
                {
                    Log.Warning($"{location}: Data was Incomplete");
                    return BadRequest(ModelState);
                }
                var book = _mapper.Map<Book>(bookDTO);
                var isSuccess = await _bookRepository.Update(book);
                if (!isSuccess)
                    return InternalError($"{location}: Update failed for record with id: {id}");

                Log.Information($"{location}: Record with id: {id} successfully updated");
                return NoContent();
            }
            catch (Exception ex)
            {
                return InternalError($"{location}: {ex.Message} - {ex.InnerException}");
            }
        }

        /// <summary>
        /// Removes a book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            var location = GetControllerAction();
            try
            {
                Log.Information($"{location}: Delete Attempted on record with id: {id} ");
                if (id < 1)
                {
                    Log.Warning($"{location}: Delete failed with bad data - id: {id}");
                    return BadRequest();
                }
                var isExists = await _bookRepository.IsExists(id);
                if (!isExists)
                {
                    Log.Warning($"{location}: Failed to retrieve record with id: {id}");
                    return NotFound();
                }

                var book = await _bookRepository.FindById(id);
                var isSuccess = await _bookRepository.Delete(book);
                if (!isSuccess)
                {
                    return InternalError($"{location}: Delete failed for record with id: {id}");
                }
                Log.Information($"{location}: Record with id: {id} successfully deleted");
                return NoContent();
            }
            catch (Exception ex)
            {
                return InternalError($"{location}: {ex.Message} - {ex.InnerException}");
            }
        }
        private string GetControllerAction()
        {
            var controller = ControllerContext.ActionDescriptor.ControllerName;
            var action = ControllerContext.ActionDescriptor.ActionName;

            return $"{controller}/{action}";
        }
        private ObjectResult InternalError(string message)
        {
            Log.Error(message);
            return StatusCode(500, "Something went wrong. Please contact the Administrator ");
        }
    }
}