import assert from 'node:assert/strict'
const base = process.env.SMOKE_BASE_URL || 'http://127.0.0.1:18081'
async function get(path) {
  const response = await fetch(base + path)
  assert.equal(response.status, 200, `${path} status`)
  return response
}
assert.match(await (await get('/')).text(), /Stockroom/)
assert.equal((await (await get('/api/health')).json()).status, 'ready')
const report = await (await get('/api/reports')).json()
assert.ok(report.totalProducts >= 0)
assert.equal(report.totalUnits, report.categories.reduce((sum, category) => sum + category.units, 0))
const cents = value => BigInt(value.replace('.', ''))
assert.equal(cents(report.inventoryValue), report.categories.reduce((sum, category) => sum + cents(category.value), 0n))
assert.match((await (await get('/openapi/v1.json')).json()).openapi, /^3\./)
assert.equal((await get('/products')).headers.get('x-content-type-options'), 'nosniff')
console.log(JSON.stringify({ status: 'passed', base, totalProducts: report.totalProducts, totalUnits: report.totalUnits, inventoryValue: report.inventoryValue, checkedAt: new Date().toISOString() }, null, 2))
