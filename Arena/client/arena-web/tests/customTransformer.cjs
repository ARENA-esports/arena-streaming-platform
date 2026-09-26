const { TsJestTransformer } = require('ts-jest');

class CustomTransformer extends TsJestTransformer {
  process(sourceText, sourcePath, transformOptions) {
    const transformed = sourceText
      .replace(/\bimport\.meta\.env\b/g, 'process.env')
      .replace(/\btypeof import\.meta\b/g, "'object'")
      .replace(/\bimport\.meta\b/g, '({ env: process.env })');
    return super.process(transformed, sourcePath, transformOptions);
  }
}

module.exports = {
  createTransformer(tsJestConfig) {
    return new CustomTransformer(tsJestConfig);
  },
};
