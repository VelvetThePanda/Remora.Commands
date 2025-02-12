//
//  NamedParameterShape.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Remora.Commands.Tokenization;
using Remora.Commands.Trees;

namespace Remora.Commands.Signatures
{
    /// <summary>
    /// Represents a named parameter with a single value.
    /// </summary>
    [PublicAPI]
    public class NamedParameterShape : IParameterShape
    {
        /// <summary>
        /// Gets the short name of the parameter, if any. At least one of <see cref="ShortName"/> and
        /// <see cref="LongName"/> must be set.
        /// </summary>
        public char? ShortName { get; }

        /// <summary>
        /// Gets the long name of the parameter, if any. At least one of <see cref="ShortName"/> and
        /// <see cref="LongName"/> must be set.
        /// </summary>
        public string? LongName { get; }

        /// <inheritdoc />
        public ParameterInfo Parameter { get; }

        /// <inheritdoc/>
        public virtual object? DefaultValue => this.Parameter.DefaultValue;

        /// <inheritdoc/>
        public string HintName
        {
            get
            {
                if (this.LongName is not null)
                {
                    return this.LongName;
                }

                if (this.ShortName is not null)
                {
                    return this.ShortName.ToString() ?? throw new InvalidOperationException();
                }

                return this.Parameter.Name ?? throw new InvalidOperationException();
            }
        }

        /// <inheritdoc/>
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedParameterShape"/> class.
        /// </summary>
        /// <param name="parameter">The underlying parameter.</param>
        /// <param name="shortName">The short name.</param>
        /// <param name="longName">The long name.</param>
        /// <param name="description">The description of the parameter.</param>
        public NamedParameterShape
        (
            ParameterInfo parameter,
            char shortName,
            string longName,
            string? description = null
        )
        {
            this.Parameter = parameter;
            this.ShortName = shortName;
            this.LongName = longName;
            this.Description = description ?? Constants.DefaultDescription;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedParameterShape"/> class.
        /// </summary>
        /// <param name="parameter">The underlying parameter.</param>
        /// <param name="longName">The long name.</param>
        /// <param name="description">The description of the parameter.</param>
        public NamedParameterShape
        (
            ParameterInfo parameter,
            string longName,
            string? description = null
        )
        {
            this.Parameter = parameter;
            this.LongName = longName;
            this.Description = description ?? Constants.DefaultDescription;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedParameterShape"/> class.
        /// </summary>
        /// <param name="parameter">The underlying parameter.</param>
        /// <param name="shortName">The short name.</param>
        /// <param name="description">The description of the parameter.</param>
        public NamedParameterShape
        (
            ParameterInfo parameter,
            char shortName,
            string? description = null
        )
        {
            this.Parameter = parameter;
            this.ShortName = shortName;
            this.Description = description ?? Constants.DefaultDescription;
        }

        /// <inheritdoc/>
        public virtual bool Matches
        (
            TokenizingEnumerator tokenizer,
            out ulong consumedTokens,
            TreeSearchOptions? searchOptions = null
        )
        {
            searchOptions ??= new TreeSearchOptions();
            consumedTokens = 0;

            if (!tokenizer.MoveNext())
            {
                return false;
            }

            switch (tokenizer.Current.Type)
            {
                case TokenType.LongName:
                {
                    if (this.LongName is null)
                    {
                        return false;
                    }

                    if (!tokenizer.Current.Value.Equals(this.LongName, searchOptions.KeyComparison))
                    {
                        return false;
                    }

                    break;
                }
                case TokenType.ShortName:
                {
                    if (this.ShortName is null)
                    {
                        return false;
                    }

                    if (tokenizer.Current.Value.Length != 1)
                    {
                        return false;
                    }

                    if (tokenizer.Current.Value[0] != this.ShortName.Value)
                    {
                        return false;
                    }

                    break;
                }
                case TokenType.Value:
                {
                    return false;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            if (!tokenizer.MoveNext())
            {
                return false;
            }

            if (tokenizer.Current.Type != TokenType.Value)
            {
                return false;
            }

            consumedTokens = 2;
            return true;
        }

        /// <inheritdoc/>
        public virtual bool Matches
        (
            KeyValuePair<string, IReadOnlyList<string>> namedValue,
            out bool isFatal,
            TreeSearchOptions? searchOptions = null
        )
        {
            searchOptions ??= new TreeSearchOptions();
            isFatal = false;

            var (name, value) = namedValue;

            var nameMatches = name.Equals(this.LongName, searchOptions.KeyComparison) ||
                              (this.ShortName is not null && name.Length == 1 && name[0] == this.ShortName);

            if (!nameMatches)
            {
                return false;
            }

            if (value.Count == 1)
            {
                return true;
            }

            isFatal = true;
            return false;
        }

        /// <inheritdoc/>
        public virtual bool IsOmissible(TreeSearchOptions? searchOptions = null) => this.Parameter.IsOptional;
    }
}
