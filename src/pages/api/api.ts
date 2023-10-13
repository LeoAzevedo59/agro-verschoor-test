interface Todo {
  id: string;
  description: string;
  isChecked: boolean;
}

import axios from "axios";

const apiAxios = axios.create({
  baseURL: 'http://localhost:5000/'
});

export const api = () => {
  console.log(apiAxios.getUri());
  async function get(): Promise<Todo[]> {
    let response: Todo[] = [];
    let _status = 0;

    try {
      const { data, status } = await apiAxios.get<Todo[]>('todos');

      _status = status;
      response = data;

    } catch (error) {
      console.log(`Erro ${_status} ao obter todos.`, error);
    }

    return response;
  }

  async function insert(todo: Todo): Promise<boolean> {
    let response: boolean = true;

    try {
      await apiAxios.post('todos', todo);
    } catch (error) {
      response = false;
      console.log(error);
    }

    return response;
  }

  async function update(id: string, isChecked: boolean) {
    try {
      await apiAxios.put(`todos/${id}?isChecked=${isChecked}`);
    } catch (error) {
      console.log(error);
    }
  }

  async function remove(id: string) {
    try {
      await apiAxios.delete(`todos/${id}`);
    } catch (error) {
      console.log(error);
    }
  }

  async function configInitial(): Promise<boolean> {
    const { data } = await apiAxios.get('configInitial');
    return data;
  }

  return {
    get,
    insert,
    update,
    remove,
    configInitial
  }
}