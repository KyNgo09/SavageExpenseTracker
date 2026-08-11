using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SavageExpenseTracker.Application.Constants;
using SavageExpenseTracker.Application.Dtos;
using SavageExpenseTracker.Application.Dtos.Expense;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.WebApi.Extensions;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/expenses")]
    [Authorize]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        // GET: api/expenses/me?pageNumber=1&pageSize=10
        [HttpGet("me")]
        public async Task<ActionResult<PagedResultDto<ExpenseDto>>> GetMyExpenses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var pagedExpenses = await _expenseService.GetUserExpensesAsync(User.GetUserId(), pageNumber, pageSize);
            return Ok(pagedExpenses);
        }

        // GET: api/expenses/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseDto>> GetById(long id)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null || expense.UserId != User.GetUserId())
            {
                return NotFound(new { Message = "Expense Not Found" });
            }
            return Ok(expense);
        }

        // POST: api/expenses
        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> Create([FromBody] CreateExpenseDto createExpenseDto)
        {
            createExpenseDto.UserId = User.GetUserId();
            var createdExpense = await _expenseService.CreateExpenseAsync(createExpenseDto);
            return CreatedAtAction(nameof(GetById), new { id = createdExpense.Id }, createdExpense);
        }

        // PUT: api/expenses/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, UpdateExpenseDto updateExpenseDto)
        {
            var result = await _expenseService.UpdateExpenseAsync(id, User.GetUserId(), updateExpenseDto);
            if (!result)
            {
                return NotFound(new { Message = "Expense Not Found" });
            }
            return NoContent();
        }

        // DELETE: api/expenses/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _expenseService.DeleteExpenseAsync(id, User.GetUserId());
            if (!result)
            {
                return NotFound(new { Message = $"Can't find Expense with Id: {id}" });
            }
            return NoContent();
        }

        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadReceipt(IFormFile file, [FromServices] IPhotoService photoService)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Please select an image file!" });

            // Check max file size (10 MB)
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { Message = "File size must not exceed 10 MB!" });

            // Check Content-Type & Extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension) || !file.ContentType.StartsWith("image/"))
            {
                return BadRequest(new { Message = "Invalid image file format! Only JPG, JPEG, PNG, WEBP, GIF, and HEIC images are allowed." });
            }

            using var stream = file.OpenReadStream();
            var imageUrl = await photoService.UploadPhotoAsync(stream, file.FileName, PhotoFolders.ExpensePhotos);
            return Ok(new { ImageUrl = imageUrl });
        }
    }
}
