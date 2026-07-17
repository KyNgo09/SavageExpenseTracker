using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SavageExpenseTracker.Application.Dtos.Expense;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.WebApi.Controllers
{
    [ApiController]
    [Route("api/expenses")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        // GET: api/expenses/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetUserExpenses(Guid userId)
        {
            var expenses = await _expenseService.GetUserExpensesAsync(userId);
            return Ok(expenses);
        }

        // GET: api/expenses/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseDto>> GetById(long id)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound(new { Message = "Expense Not Found"});
            }
            return Ok(expense);
        }

        // POST: api/expenses
        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> Create(CreateExpenseDto createExpenseDto)
        {
            try
            {
                var createdExpense = await _expenseService.CreateExpenseAsync(createExpenseDto);
                return CreatedAtAction(nameof(GetById), new { id = createdExpense.Id }, createdExpense);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // PUT: api/expenses/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, UpdateExpenseDto updateExpenseDto)
        {
            try
            {
                var result = await _expenseService.UpdateExpenseAsync(id, updateExpenseDto);
                if (!result)
                {
                    return NotFound(new { Message = "Exp" });
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // DELETE: api/expenses/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _expenseService.DeleteExpenseAsync(id);
                if (!result)
                {
                    return NotFound(new { Message = $"Can't find Expense with Id: {id}" });
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
