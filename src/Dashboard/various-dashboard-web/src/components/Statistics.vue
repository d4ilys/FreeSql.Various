<script setup lang="ts">
const nowDateTime = ref(new Date().toLocaleString())
//

</script>

<template>

    <n-page-header subtitle="测试版 0.0.1">
      <n-grid :cols="5">
        <n-gi>
          <n-statistic label="数据库数量" value="25"/>
        </n-gi>
        <n-gi>
          <n-statistic label="活跃" value="20"/>
        </n-gi>
        <n-gi>
          <n-statistic label="空闲" value="5"/>
        </n-gi>
        <n-gi>
          <n-statistic label="本地消息表" value="83 个"/>
        </n-gi>
        <n-gi>
          <n-statistic label="多库事务" value="2,346 个"/>
        </n-gi>
      </n-grid>
      <template #title>
        🌟FreeSql.Various
      </template>
      <template #header>
        <n-breadcrumb>
          <n-breadcrumb-item>本地消息表日志</n-breadcrumb-item>
          <n-breadcrumb-item>多库事务日志</n-breadcrumb-item>
          <n-breadcrumb-item>数据库统计</n-breadcrumb-item>
        </n-breadcrumb>
      </template>
      <template #extra>
        <n-space>
          <n-switch v-model:value="active">
            <template #checked>
              暗黑模式
            </template>
            <template #unchecked>
              明亮模式
            </template>
          </n-switch>
        </n-space>
      </template>
      <template #footer>
        {{ nowDateTime }}
      </template>
    </n-page-header>
</template>

<style scoped>

</style>