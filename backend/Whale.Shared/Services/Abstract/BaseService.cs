﻿using AutoMapper;
using Whale.DAL;

namespace Whale.Shared.Services.Abstract
{
    public abstract class BaseService
    {
        protected readonly WhaleDbContext _context;
        protected readonly IMapper _mapper;

        protected BaseService(WhaleDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
    }
}
