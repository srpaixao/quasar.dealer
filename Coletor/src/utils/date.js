export const formatDateTimeBr = (value, options = null) => {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);

  const formatOptions = options || {
    dateStyle: 'short',
    timeStyle: 'medium'
  };

  if (formatOptions.dateStyle && formatOptions.timeStyle) {
    const { dateStyle, timeStyle, ...rest } = formatOptions;
    const dateText = new Intl.DateTimeFormat('pt-BR', {
      ...rest,
      dateStyle
    }).format(date);
    const timeText = new Intl.DateTimeFormat('pt-BR', {
      ...rest,
      timeStyle
    }).format(date);
    return `${dateText} ${timeText}`.trim();
  }

  return new Intl.DateTimeFormat('pt-BR', formatOptions).format(date);
};
