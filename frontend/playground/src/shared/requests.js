export function buildRequest(tool, values) {
  const method = tool.method ?? "POST";
  const path = tool.path.replace(/\{([A-Za-z]+)\}/g, (_, key) => {
    const value = values[key];
    if (value === undefined || value === null || String(value) === "")
      throw new Error("Complete os campos para gerar a requisição.");
    return encodeURIComponent(String(value));
  });
  return {
    method,
    url: "/api/v1/" + path,
    ...(method === "POST" ? { body: values } : {})
  };
}
