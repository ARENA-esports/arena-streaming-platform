/** @type {import('ts-jest').JestConfigWithTsJest} */
module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'jsdom',
  roots: ['<rootDir>/src', '<rootDir>/tests'],
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
  },
  setupFilesAfterEnv: ['<rootDir>/tests/setupTests.ts'],
  // Explicitly allow ALL project source files to be transformed (override Jest 30 defaults)
  transformIgnorePatterns: ['/node_modules/'],
  transform: {
    '^.+\\.tsx?$': [
      '<rootDir>/tests/customTransformer.cjs',
      {
        tsconfig: {
          jsx: 'react-jsx',
          esModuleInterop: true,
          allowSyntheticDefaultImports: true,
          module: 'commonjs',
          target: 'ES2020',
        },
        diagnostics: {
          ignoreCodes: [1343],
        },
      },
    ],
  },
};
