using IlkProjem.DAL.Interfaces;
using IlkProjem.Core.Models;
using IlkProjem.Core.Dtos.CustomerDtos;
using IlkProjem.BLL.Interfaces;
using IlkProjem.Core.Utilities.Results;
using Microsoft.Extensions.Localization;
using IlkProjem.Core.Resources;
using IlkProjem.Core.Dtos.SpecificationDtos;
using FluentValidation;

namespace IlkProjem.BLL.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IStringLocalizer<Messages> _localizer;
    private readonly IValidator<CustomerCreateDto> _createValidator;
    private readonly IValidator<CustomerUpdateDto> _updateValidator;
    private readonly IValidator<CustomerDeleteDto> _deleteValidator;

    public CustomerService(
        ICustomerRepository repository,
        IStringLocalizer<Messages> localizer,
        IValidator<CustomerCreateDto> createValidator,
        IValidator<CustomerUpdateDto> updateValidator,
        IValidator<CustomerDeleteDto> deleteValidator)
    {
        _repository = repository;
        _localizer = localizer;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _deleteValidator = deleteValidator;
    }

    public async Task<IResult> AddCustomer(CustomerCreateDto createDto, CancellationToken ct = default)
    {
        var validationResult = await _createValidator.ValidateAsync(createDto, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new ErrorResult(errors);
        }

        var customer = new Customer { Name = createDto.Name, Email = createDto.Email };
        await _repository.AddAsync(customer, ct);
        
        return new SuccessResult(_localizer["CustomerAdded"]); 
    }

    public async Task<IDataResult<List<CustomerReadDto>>> GetCustomersAsync(CustomerSpecParams custParams, CancellationToken ct = default)
    {
        var spec = new CustomerCursorSpecification(custParams);
        var customers = await _repository.ListAsync(spec, ct);

        var customerDtos = customers.Select(c => new CustomerReadDto
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Balance = c.Balance,
            CreatedAt = c.CreatedAt,
            ProfileImageId = c.ProfileImageId
        }).ToList();

        return new SuccessDataResult<List<CustomerReadDto>>(customerDtos, "Customers retrieved");
    }

    public async Task<IDataResult<CustomerReadDto>> GetCustomerById(int id, CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(id, ct);
        if (customer == null) 
            return new ErrorDataResult<CustomerReadDto>(_localizer["CustomerNotFound"]);

        var data = new CustomerReadDto { 
            Id = customer.Id, 
            Name = customer.Name, 
            Email = customer.Email, 
            Balance = customer.Balance,
            CreatedAt = customer.CreatedAt,
            ProfileImageId = customer.ProfileImageId,
            ProfileImagePath = customer.ProfileImage?.RelativePath
        };
        return new SuccessDataResult<CustomerReadDto>(data);
    }

    public async Task<IResult> UpdateCustomer(CustomerUpdateDto updateDto, CancellationToken ct = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(updateDto, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new ErrorResult(errors);
        }

        var existingCustomer = await _repository.GetByIdAsync(updateDto.Id, ct);
        if (existingCustomer == null) 
            return new ErrorResult(_localizer["CustomerNotFound"]);
        
        existingCustomer.Name = updateDto.Name;
        existingCustomer.Email = updateDto.Email;
        existingCustomer.Balance = updateDto.Balance;
        existingCustomer.TcKimlikNo = updateDto.TcKimlikNo;

        await _repository.UpdateAsync(existingCustomer, ct);
        return new SuccessResult(_localizer["CustomerUpdated"]);
    }

    public async Task<IResult> DeleteCustomer(CustomerDeleteDto deleteDto, CancellationToken ct = default)
    {
        var validationResult = await _deleteValidator.ValidateAsync(deleteDto, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new ErrorResult(errors);
        }

        var deleted = await _repository.DeleteAsync(deleteDto.Id, ct);
        if (!deleted) return new ErrorResult(_localizer["DeleteError"]);

        return new SuccessResult(_localizer["CustomerDeleted"]);
    }
}